using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace SistemTokoInventori
{
    /// <summary>
    /// Fase 3 — helper stok multi-lokasi + batch HPP (FIFO/LIFO).
    ///
    /// Semua method menerima koneksi + transaksi yang sedang berjalan dari
    /// pemanggil dan TIDAK PERNAH membuka koneksi sendiri, supaya setiap
    /// perubahan stok ikut dalam transaksi pemanggil (gagal di mana pun =
    /// rollback menyeluruh). Transaksi boleh null kalau pemanggil hanya
    /// membaca (GetHPPMethod / StokLokasiQty).
    ///
    /// Invarian yang dijaga oleh semua pemanggil:
    ///   Item.Stok == SUM(StokLokasi.Qty) untuk item yang melacak stok
    ///   (Fisik/Rakitan), dan SUM(StokBatch.QtySisa) == StokLokasi.Qty
    ///   per (item, lokasi). Setiap stok-masuk membuat batch, setiap
    ///   stok-keluar mengonsumsi batch — tanpa pengecualian.
    ///
    /// HPP: FIFO = batch tertua (TanggalMasuk, lalu ID_Batch) dipakai
    /// dulu; LIFO = batch terbaru dulu. Metode dibaca dari Pengaturan
    /// (kunci HPPMethod, default 'FIFO').
    /// </summary>
    public static class StokHelper
    {
        /// <summary>ID lokasi utama (GUDANG UTAMA) — lokasi default untuk
        /// Kasir, terima PO, tukar tambah, dan semua alur Fase 1/2.</summary>
        public const int LokasiUtama = 1;

        /// <summary>Rincian satu batch yang terpakai saat konsumsi.</summary>
        public class KonsumsiBatchInfo
        {
            public long IdBatch;
            public decimal Qty;
            public decimal HargaBeli;
        }

        /// <summary>Hasil konsumsi batch: total HPP + rincian per batch
        /// (rincian dipakai KembalikanStok untuk mengembalikan batch yang
        /// sudah terpakai kalau langkah 2 Kasir gagal).</summary>
        public class HasilKonsumsi
        {
            public decimal TotalHPP;
            public List<KonsumsiBatchInfo> Rincian = new List<KonsumsiBatchInfo>();
        }

        private static SQLiteCommand BuatCmd(SQLiteConnection conn,
            SQLiteTransaction trans, string sql)
        {
            return new SQLiteCommand(sql, conn, trans);
        }

        /// <summary>Metode HPP aktif ('FIFO' / 'LIFO'). Nilai selain itu
        /// atau tidak ada pengaturannya dianggap 'FIFO'.</summary>
        public static string GetHPPMethod(SQLiteConnection conn, SQLiteTransaction trans)
        {
            try
            {
                using (var cmd = BuatCmd(conn, trans,
                    "SELECT Nilai FROM Pengaturan WHERE Kunci = 'HPPMethod'"))
                {
                    object hasil = cmd.ExecuteScalar();
                    string nilai = hasil == null ? "" : Convert.ToString(hasil).Trim().ToUpperInvariant();
                    return nilai == "LIFO" ? "LIFO" : "FIFO";
                }
            }
            catch
            {
                // Tabel Pengaturan belum ada (database sangat lama):
                // anggap FIFO supaya alur tidak berhenti.
                return "FIFO";
            }
        }

        /// <summary>Simpan metode HPP ('FIFO' / 'LIFO') ke Pengaturan.</summary>
        public static void SetHPPMethod(SQLiteConnection conn, string metode)
        {
            if (metode != "LIFO") metode = "FIFO";
            using (var cmd = new SQLiteCommand(
                "INSERT OR REPLACE INTO Pengaturan (Kunci, Nilai) " +
                "VALUES ('HPPMethod', @Nilai)", conn))
            {
                cmd.Parameters.AddWithValue("@Nilai", metode);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Stok satu item di satu lokasi (0 kalau belum ada baris
        /// StokLokasi — mis. item baru yang stoknya masih 0).</summary>
        public static decimal StokLokasiQty(SQLiteConnection conn,
            SQLiteTransaction trans, int idItem, int idLokasi)
        {
            using (var cmd = BuatCmd(conn, trans,
                "SELECT Qty FROM StokLokasi WHERE ID_Lokasi = @Lokasi AND ID_Item = @ID"))
            {
                cmd.Parameters.AddWithValue("@Lokasi", idLokasi);
                cmd.Parameters.AddWithValue("@ID", idItem);
                object hasil = cmd.ExecuteScalar();
                return hasil == null ? 0m : Convert.ToDecimal(hasil);
            }
        }

        /// <summary>
        /// Naikkan stok: Item.Stok += qty DAN StokLokasi += qty (upsert —
        /// baris StokLokasi dibuat kalau belum ada). Mengembalikan stok
        /// lokasi sesudah penambahan (untuk StokMovement.StokSesudah).
        /// </summary>
        public static decimal NaikkanStok(SQLiteConnection conn,
            SQLiteTransaction trans, int idItem, int idLokasi, decimal qty)
        {
            using (var cmd = BuatCmd(conn, trans,
                "UPDATE Item SET Stok = Stok + @Qty WHERE ID_Item = @ID"))
            {
                cmd.Parameters.AddWithValue("@Qty", qty);
                cmd.Parameters.AddWithValue("@ID", idItem);
                cmd.ExecuteNonQuery();
            }
            NaikkanStokLokasi(conn, trans, idItem, idLokasi, qty);
            return StokLokasiQty(conn, trans, idItem, idLokasi);
        }

        /// <summary>Naikkan StokLokasi saja (upsert). Dipakai kalau
        /// Item.Stok sudah diubah pemanggil sendiri (mis. KembalikanStok
        /// Kasir yang butuh cek rowcount UPDATE Item-nya).</summary>
        public static void NaikkanStokLokasi(SQLiteConnection conn,
            SQLiteTransaction trans, int idItem, int idLokasi, decimal qty)
        {
            using (var cmd = BuatCmd(conn, trans,
                "UPDATE StokLokasi SET Qty = Qty + @Qty " +
                "WHERE ID_Lokasi = @Lokasi AND ID_Item = @ID"))
            {
                cmd.Parameters.AddWithValue("@Qty", qty);
                cmd.Parameters.AddWithValue("@Lokasi", idLokasi);
                cmd.Parameters.AddWithValue("@ID", idItem);
                if (cmd.ExecuteNonQuery() == 0)
                {
                    using (var ins = BuatCmd(conn, trans,
                        "INSERT INTO StokLokasi (ID_Lokasi, ID_Item, Qty) " +
                        "VALUES (@Lokasi, @ID, @Qty)"))
                    {
                        ins.Parameters.AddWithValue("@Lokasi", idLokasi);
                        ins.Parameters.AddWithValue("@ID", idItem);
                        ins.Parameters.AddWithValue("@Qty", qty);
                        ins.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>
        /// Turunkan stok lokasi saja, dengan pengaman: hanya berhasil kalau
        /// StokLokasi.Qty &gt;= qty. Mengembalikan stok lokasi sesudah
        /// pengurangan, atau null kalau stok lokasi tidak cukup.
        /// </summary>
        public static decimal? TurunkanStokLokasi(SQLiteConnection conn,
            SQLiteTransaction trans, int idItem, int idLokasi, decimal qty)
        {
            using (var cmd = BuatCmd(conn, trans,
                "UPDATE StokLokasi SET Qty = Qty - @Qty " +
                "WHERE ID_Lokasi = @Lokasi AND ID_Item = @ID AND Qty >= @Qty"))
            {
                cmd.Parameters.AddWithValue("@Qty", qty);
                cmd.Parameters.AddWithValue("@Lokasi", idLokasi);
                cmd.Parameters.AddWithValue("@ID", idItem);
                if (cmd.ExecuteNonQuery() == 0) return null;
            }
            return StokLokasiQty(conn, trans, idItem, idLokasi);
        }

        /// <summary>
        /// Turunkan stok total + lokasi sekaligus (Item.Stok dan
        /// StokLokasi, masing-masing dengan guard &gt;= qty). Mengembalikan
        /// stok lokasi sesudah pengurangan, atau null kalau stok tidak
        /// cukup di salah satu level. Dipakai alur Fase 3 (konsinyasi
        /// keluar, perakitan, transfer); Kasir TIDAK memakai ini karena
        /// UPDATE Item-nya harus tetap tertulis di KasirControl (cek B1).
        /// </summary>
        public static decimal? TurunkanStok(SQLiteConnection conn,
            SQLiteTransaction trans, int idItem, int idLokasi, decimal qty)
        {
            using (var cmd = BuatCmd(conn, trans,
                "UPDATE Item SET Stok = Stok - @Qty " +
                "WHERE ID_Item = @ID AND Stok >= @Qty"))
            {
                cmd.Parameters.AddWithValue("@Qty", qty);
                cmd.Parameters.AddWithValue("@ID", idItem);
                if (cmd.ExecuteNonQuery() == 0) return null;
            }
            return TurunkanStokLokasi(conn, trans, idItem, idLokasi, qty);
        }

        /// <summary>
        /// Set stok lokasi ke nilai tertentu (upsert). Dipakai stok opname
        /// dan penyesuaian stok via Master Data.
        /// </summary>
        public static void SetStokLokasi(SQLiteConnection conn,
            SQLiteTransaction trans, int idItem, int idLokasi, decimal qty)
        {
            using (var cmd = BuatCmd(conn, trans,
                "UPDATE StokLokasi SET Qty = @Qty " +
                "WHERE ID_Lokasi = @Lokasi AND ID_Item = @ID"))
            {
                cmd.Parameters.AddWithValue("@Qty", qty);
                cmd.Parameters.AddWithValue("@Lokasi", idLokasi);
                cmd.Parameters.AddWithValue("@ID", idItem);
                if (cmd.ExecuteNonQuery() == 0)
                {
                    using (var ins = BuatCmd(conn, trans,
                        "INSERT INTO StokLokasi (ID_Lokasi, ID_Item, Qty) " +
                        "VALUES (@Lokasi, @ID, @Qty)"))
                    {
                        ins.Parameters.AddWithValue("@Lokasi", idLokasi);
                        ins.Parameters.AddWithValue("@ID", idItem);
                        ins.Parameters.AddWithValue("@Qty", qty);
                        ins.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>Buat satu batch stok-masuk.</summary>
        public static void BuatBatch(SQLiteConnection conn,
            SQLiteTransaction trans, int idItem, int idLokasi, string tanggal,
            decimal qty, decimal hargaBeli, string refType, object refId)
        {
            using (var cmd = BuatCmd(conn, trans,
                "INSERT INTO StokBatch (ID_Item, ID_Lokasi, TanggalMasuk, " +
                "QtyAwal, QtySisa, HargaBeli, RefType, RefID) " +
                "VALUES (@ID_Item, @ID_Lokasi, @Tanggal, @Qty, @Qty, " +
                "@HargaBeli, @RefType, @RefID)"))
            {
                cmd.Parameters.AddWithValue("@ID_Item", idItem);
                cmd.Parameters.AddWithValue("@ID_Lokasi", idLokasi);
                cmd.Parameters.AddWithValue("@Tanggal", tanggal);
                cmd.Parameters.AddWithValue("@Qty", qty);
                cmd.Parameters.AddWithValue("@HargaBeli", hargaBeli);
                cmd.Parameters.AddWithValue("@RefType", (object)refType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RefID", refId ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Konsumsi batch untuk stok-keluar sejumlah qty, sesuai metode
        /// (FIFO = tertua dulu, LIFO = terbaru dulu). Mengembalikan total
        /// HPP + rincian per batch. MELEMPAR InvalidOperationException
        /// kalau total QtySisa batch &lt; qty (data batch tidak konsisten
        /// dengan stok) — pemanggil harus me-rollback transaksinya.
        /// </summary>
        public static HasilKonsumsi KonsumsiBatch(SQLiteConnection conn,
            SQLiteTransaction trans, int idItem, int idLokasi, decimal qty,
            string metode)
        {
            HasilKonsumsi hasil = new HasilKonsumsi();
            string urutan = metode == "LIFO"
                ? "TanggalMasuk DESC, ID_Batch DESC"
                : "TanggalMasuk ASC, ID_Batch ASC";
            decimal sisa = qty;
            using (var cmd = BuatCmd(conn, trans,
                "SELECT ID_Batch, QtySisa, HargaBeli FROM StokBatch " +
                "WHERE ID_Item = @ID AND ID_Lokasi = @Lokasi AND QtySisa > 0 " +
                "ORDER BY " + urutan))
            {
                cmd.Parameters.AddWithValue("@ID", idItem);
                cmd.Parameters.AddWithValue("@Lokasi", idLokasi);
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    // Reader harus ditutup sebelum UPDATE di koneksi yang
                    // sama (SQLite tidak mengizinkan 2 statement aktif
                    // bersamaan) — jadi tampung dulu daftar batch-nya.
                    List<KonsumsiBatchInfo> daftar = new List<KonsumsiBatchInfo>();
                    while (reader.Read())
                    {
                        daftar.Add(new KonsumsiBatchInfo
                        {
                            IdBatch = Convert.ToInt64(reader["ID_Batch"]),
                            Qty = Convert.ToDecimal(reader["QtySisa"]),
                            HargaBeli = Convert.ToDecimal(reader["HargaBeli"])
                        });
                    }
                    reader.Close();
                    foreach (KonsumsiBatchInfo batch in daftar)
                    {
                        if (sisa <= 0) break;
                        decimal ambil = batch.Qty < sisa ? batch.Qty : sisa;
                        using (var upd = BuatCmd(conn, trans,
                            "UPDATE StokBatch SET QtySisa = QtySisa - @Ambil " +
                            "WHERE ID_Batch = @ID"))
                        {
                            upd.Parameters.AddWithValue("@Ambil", ambil);
                            upd.Parameters.AddWithValue("@ID", batch.IdBatch);
                            upd.ExecuteNonQuery();
                        }
                        hasil.Rincian.Add(new KonsumsiBatchInfo
                        {
                            IdBatch = batch.IdBatch,
                            Qty = ambil,
                            HargaBeli = batch.HargaBeli
                        });
                        hasil.TotalHPP += ambil * batch.HargaBeli;
                        sisa -= ambil;
                    }
                }
            }
            if (sisa > 0)
            {
                throw new InvalidOperationException(
                    "Batch stok item ID " + idItem + " tidak cukup " +
                    "(kurang " + sisa + "): data batch tidak konsisten " +
                    "dengan stok. Transaksi dibatalkan.");
            }
            return hasil;
        }

        /// <summary>
        /// Kembalikan batch yang sudah terpakai (kompensasi B1 di Kasir):
        /// QtySisa tiap batch ditambah kembali sesuai rincian konsumsi.
        /// </summary>
        public static void KembalikanBatch(SQLiteConnection conn,
            SQLiteTransaction trans, List<KonsumsiBatchInfo> rincian)
        {
            if (rincian == null) return;
            foreach (KonsumsiBatchInfo batch in rincian)
            {
                using (var cmd = BuatCmd(conn, trans,
                    "UPDATE StokBatch SET QtySisa = QtySisa + @Qty " +
                    "WHERE ID_Batch = @ID"))
                {
                    cmd.Parameters.AddWithValue("@Qty", batch.Qty);
                    cmd.Parameters.AddWithValue("@ID", batch.IdBatch);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Pindahkan batch antar lokasi (transfer stok): batch di lokasi
        /// asal dikonsumsi tertua dulu, lalu dibuat batch baru di lokasi
        /// tujuan dengan HargaBeli dan TanggalMasuk yang sama (supaya
        /// urutan FIFO di tujuan tetap benar). Mengembalikan total HPP
        /// yang dipindah + rincian per batch asal.
        /// </summary>
        public static HasilKonsumsi PindahkanBatch(SQLiteConnection conn,
            SQLiteTransaction trans, int idItem, int idLokasiAsal,
            int idLokasiTujuan, decimal qty, object refId)
        {
            HasilKonsumsi hasil = new HasilKonsumsi();
            decimal sisa = qty;
            List<KonsumsiBatchInfo> daftar = new List<KonsumsiBatchInfo>();
            List<string> tanggalMasuk = new List<string>();
            using (var cmd = BuatCmd(conn, trans,
                "SELECT ID_Batch, QtySisa, HargaBeli, TanggalMasuk FROM StokBatch " +
                "WHERE ID_Item = @ID AND ID_Lokasi = @Lokasi AND QtySisa > 0 " +
                "ORDER BY TanggalMasuk ASC, ID_Batch ASC"))
            {
                cmd.Parameters.AddWithValue("@ID", idItem);
                cmd.Parameters.AddWithValue("@Lokasi", idLokasiAsal);
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        daftar.Add(new KonsumsiBatchInfo
                        {
                            IdBatch = Convert.ToInt64(reader["ID_Batch"]),
                            Qty = Convert.ToDecimal(reader["QtySisa"]),
                            HargaBeli = Convert.ToDecimal(reader["HargaBeli"])
                        });
                        tanggalMasuk.Add(Convert.ToString(reader["TanggalMasuk"]));
                    }
                }
            }
            for (int i = 0; i < daftar.Count && sisa > 0; i++)
            {
                decimal ambil = daftar[i].Qty < sisa ? daftar[i].Qty : sisa;
                using (var upd = BuatCmd(conn, trans,
                    "UPDATE StokBatch SET QtySisa = QtySisa - @Ambil " +
                    "WHERE ID_Batch = @ID"))
                {
                    upd.Parameters.AddWithValue("@Ambil", ambil);
                    upd.Parameters.AddWithValue("@ID", daftar[i].IdBatch);
                    upd.ExecuteNonQuery();
                }
                BuatBatch(conn, trans, idItem, idLokasiTujuan,
                    tanggalMasuk[i], ambil, daftar[i].HargaBeli,
                    "Transfer", refId);
                hasil.Rincian.Add(new KonsumsiBatchInfo
                {
                    IdBatch = daftar[i].IdBatch,
                    Qty = ambil,
                    HargaBeli = daftar[i].HargaBeli
                });
                hasil.TotalHPP += ambil * daftar[i].HargaBeli;
                sisa -= ambil;
            }
            if (sisa > 0)
            {
                throw new InvalidOperationException(
                    "Batch stok item ID " + idItem + " di lokasi asal tidak " +
                    "cukup (kurang " + sisa + "). Transaksi dibatalkan.");
            }
            return hasil;
        }

        /// <summary>
        /// Tulis satu baris StokMovement. idLokasi boleh null (untuk
        /// pergerakan yang tidak menyentuh stok lokasi mana pun — mis.
        /// penjualan titipan konsinyasi masuk).
        /// </summary>
        public static void TulisMovement(SQLiteConnection conn,
            SQLiteTransaction trans, int idItem, int? idLokasi, string tanggal,
            string jenis, decimal qty, decimal stokSesudah, string refId,
            string keterangan)
        {
            using (var cmd = BuatCmd(conn, trans,
                "INSERT INTO StokMovement (ID_Item, ID_Lokasi, Tanggal, Jenis, " +
                "Qty, StokSesudah, RefID, Keterangan) " +
                "VALUES (@ID_Item, @ID_Lokasi, @Tanggal, @Jenis, @Qty, " +
                "@StokSesudah, @RefID, @Keterangan)"))
            {
                cmd.Parameters.AddWithValue("@ID_Item", idItem);
                cmd.Parameters.AddWithValue("@ID_Lokasi",
                    idLokasi.HasValue ? (object)idLokasi.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@Tanggal", tanggal);
                cmd.Parameters.AddWithValue("@Jenis", jenis);
                cmd.Parameters.AddWithValue("@Qty", qty);
                cmd.Parameters.AddWithValue("@StokSesudah", stokSesudah);
                cmd.Parameters.AddWithValue("@RefID", (object)refId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Keterangan", (object)keterangan ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
