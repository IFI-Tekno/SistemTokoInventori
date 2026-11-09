using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace SistemTokoInventori
{
    /// <summary>
    /// Fase 4 — mesin jurnal otomatis (double-entry) untuk Keuangan.
    ///
    /// Semua method menerima koneksi + transaksi yang sedang berjalan dari
    /// pemanggil dan TIDAK PERNAH membuka koneksi sendiri, supaya jurnal
    /// ikut dalam transaksi operasi bisnisnya (gagal di mana pun =
    /// rollback menyeluruh — tidak ada jurnal yatim dan tidak ada operasi
    /// tanpa jurnal). Setiap jurnal HARUS seimbang (SUM Debit == SUM
    /// Kredit); kalau tidak, TulisJurnal melempar InvalidOperationException
    /// dan transaksi pemanggil di-rollback.
    ///
    /// Akun dicari dari bagan akun (tabel Akun) berdasarkan Kode — kalau
    /// kode tidak ada, melempar (jangan diam-diam melewatkan baris).
    /// Baris bernilai 0 dilewati (tidak ditulis) supaya jurnal tetap
    /// ringkas; kalau setelah itu tidak ada baris tersisa, tidak ada
    /// jurnal yang dibuat (return 0).
    ///
    /// Aturan per alur (saldo akun memakai SaldoNormal-nya):
    /// - Penjualan Kasir: Kas masuk = Bayar - Kembalian (uang yang
    ///   benar-benar diterima; kembalian bukan pendapatan). Pendapatan =
    ///   subtotal - diskon. Poin dipakai &amp; tukar tambah &amp; deposit
    ///   adalah cara bayar (bukan potongan harga): Kas masuk + Piutang
    ///   + Deposit + Poin + Tukar = Pendapatan, jadi jurnal seimbang.
    /// - Terima PO: Persediaan = Total; Kas keluar = DP; sisanya Hutang
    ///   Usaha. Kalau DP &gt; Total, kelebihannya masuk akun
    ///   '1104 Uang Muka Pembelian' (aktiva) supaya tetap seimbang.
    /// - Bayar Hutang: Hutang Usaha (atau Hutang Konsinyasi kalau
    ///   RefType hutangnya 'KonsinyasiMasuk') berkurang, Kas berkurang.
    /// - Terima Piutang: Kas bertambah, Piutang Usaha berkurang.
    /// - Jual Konsinyasi Masuk: Kas masuk + Pendapatan; HPP lawan
    ///   Hutang Konsinyasi (barang milik supplier, bukan persediaan toko).
    /// - Jual Konsinyasi Keluar (agen): Piutang lawan Pendapatan;
    ///   HPP lawan Persediaan (stok sudah keluar saat dititipkan).
    /// - Opname: surplus -&gt; Persediaan lawan Pendapatan Lain-lain;
    ///   shortfall (nilai HPP dari batch yang terpakai) -&gt; Biaya
    ///   Selisih Persediaan lawan Persediaan.
    /// - Biaya manual / Deposit terima / Deposit kembali: Kas lawan
    ///   akun biaya / Deposit Pelanggan.
    /// - Perakitan &amp; Transfer: TIDAK menjurnal (transformasi aset di
    ///   dalam Persediaan / antar lokasi — nilai total tidak berubah).
    ///
    /// Transaksi Fase 1-3 TIDAK di-backfill ke jurnal: jurnal dimulai
    /// dari go-live Fase 4. Neraca &amp; Buku Besar memakai SaldoAwal akun
    /// + jurnal Fase 4 (lihat V2_NOTES §10).
    /// </summary>
    public static class JurnalHelper
    {
        /// <summary>Satu baris jurnal: akun (dicari dari Kode) + jumlah
        /// Debit / Kredit. Baris dengan Debit dan Kredit sama-sama 0
        /// dilewati saat penulisan.</summary>
        public class BarisJurnal
        {
            public string KodeAkun;
            public decimal Debit;
            public decimal Kredit;

            public BarisJurnal(string kodeAkun, decimal debit, decimal kredit)
            {
                KodeAkun = kodeAkun;
                Debit = debit;
                Kredit = kredit;
            }
        }

        private static SQLiteCommand BuatCmd(SQLiteConnection conn,
            SQLiteTransaction trans, string sql)
        {
            return new SQLiteCommand(sql, conn, trans);
        }

        /// <summary>ID akun dari Kode bagan akun. Melempar kalau kode
        /// tidak ada — jurnal tidak boleh diam-diam kehilangan baris.</summary>
        public static long IdAkun(SQLiteConnection conn, SQLiteTransaction trans,
            string kode)
        {
            using (var cmd = BuatCmd(conn, trans,
                "SELECT ID_Akun FROM Akun WHERE Kode = @Kode"))
            {
                cmd.Parameters.AddWithValue("@Kode", kode);
                object hasil = cmd.ExecuteScalar();
                if (hasil == null)
                    throw new InvalidOperationException(
                        "Akun '" + kode + "' tidak ada di bagan akun.");
                return (long)hasil;
            }
        }

        /// <summary>
        /// Tulis satu jurnal (header + detail) dalam transaksi pemanggil.
        /// Validasi: minimal 1 baris bernilai &gt; 0 dan
        /// SUM(Debit) == SUM(Kredit) (toleransi Rp 0,01 untuk pembulatan).
        /// Melempar InvalidOperationException kalau tidak seimbang.
        /// Mengembalikan ID_Jurnal, atau 0 kalau semua baris bernilai 0
        /// (tidak ada jurnal yang dibuat).
        /// </summary>
        public static long TulisJurnal(SQLiteConnection conn,
            SQLiteTransaction trans, string tanggal, string noBukti,
            string keterangan, string refType, object refId,
            List<BarisJurnal> baris)
        {
            List<BarisJurnal> isi = new List<BarisJurnal>();
            decimal totalDebit = 0;
            decimal totalKredit = 0;
            foreach (BarisJurnal b in baris)
            {
                if (b.Debit == 0 && b.Kredit == 0) continue;
                if (b.Debit < 0 || b.Kredit < 0)
                    throw new InvalidOperationException(
                        "Jurnal " + noBukti + ": nilai Debit/Kredit tidak " +
                        "boleh negatif (" + b.KodeAkun + ").");
                isi.Add(b);
                totalDebit += b.Debit;
                totalKredit += b.Kredit;
            }
            if (isi.Count == 0) return 0;
            if (Math.Abs(totalDebit - totalKredit) > 0.01m)
                throw new InvalidOperationException(
                    "Jurnal " + noBukti + " tidak seimbang: Debit Rp " +
                    totalDebit.ToString("N0") + " != Kredit Rp " +
                    totalKredit.ToString("N0") + ". Transaksi dibatalkan.");
            long idJurnal;
            using (var cmd = BuatCmd(conn, trans,
                "INSERT INTO Jurnal (Tanggal, NoBukti, Keterangan, RefType, RefID) " +
                "VALUES (@Tanggal, @NoBukti, @Keterangan, @RefType, @RefID)"))
            {
                cmd.Parameters.AddWithValue("@Tanggal", tanggal);
                cmd.Parameters.AddWithValue("@NoBukti", (object)noBukti ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Keterangan", (object)keterangan ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RefType", (object)refType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RefID", refId ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            using (var cmd = BuatCmd(conn, trans, "SELECT last_insert_rowid()"))
            {
                idJurnal = (long)cmd.ExecuteScalar();
            }
            foreach (BarisJurnal b in isi)
            {
                long idAkun = IdAkun(conn, trans, b.KodeAkun);
                using (var cmd = BuatCmd(conn, trans,
                    "INSERT INTO JurnalDetail (ID_Jurnal, ID_Akun, Debit, Kredit) " +
                    "VALUES (@ID_Jurnal, @ID_Akun, @Debit, @Kredit)"))
                {
                    cmd.Parameters.AddWithValue("@ID_Jurnal", idJurnal);
                    cmd.Parameters.AddWithValue("@ID_Akun", idAkun);
                    cmd.Parameters.AddWithValue("@Debit", b.Debit);
                    cmd.Parameters.AddWithValue("@Kredit", b.Kredit);
                    cmd.ExecuteNonQuery();
                }
            }
            return idJurnal;
        }

        /// <summary>
        /// Penjualan Kasir (dipanggil di langkah 2, dalam transaksi yang
        /// sama). kasMasuk = Bayar - Kembalian (uang yang benar-benar
        /// diterima). Bukti: Debit Kas [kasMasuk]; Debit Piutang Usaha
        /// [sisaPiutang]; Debit Deposit Pelanggan [depositDipakai]; Debit
        /// Biaya Promosi &amp; Poin [nilaiPoin]; Debit Persediaan
        /// [nilaiTukar]; Kredit Pendapatan Penjualan [pendapatan]; Debit
        /// HPP [totalHPP]; Kredit Persediaan [totalHPP].
        /// Seimbang karena kasMasuk + sisaPiutang = Total dan
        /// Total + deposit + poin + tukar = pendapatan.
        /// </summary>
        public static long TulisJurnalPenjualan(SQLiteConnection conn,
            SQLiteTransaction trans, string tanggal, long idTransaksi,
            decimal pendapatan, decimal kasMasuk, decimal sisaPiutang,
            decimal depositDipakai, decimal nilaiPoin, decimal nilaiTukar,
            decimal totalHPP)
        {
            return TulisJurnal(conn, trans, tanggal, "INV-" + idTransaksi,
                "Penjualan tunai/tempo", "Penjualan", idTransaksi,
                new List<BarisJurnal>
                {
                    new BarisJurnal("1101", kasMasuk, 0),
                    new BarisJurnal("1102", sisaPiutang, 0),
                    new BarisJurnal("2103", depositDipakai, 0),
                    new BarisJurnal("6105", nilaiPoin, 0),
                    new BarisJurnal("1103", nilaiTukar, 0),
                    new BarisJurnal("4101", 0, pendapatan),
                    new BarisJurnal("5101", totalHPP, 0),
                    new BarisJurnal("1103", 0, totalHPP)
                });
        }

        /// <summary>
        /// Terima PO (dipanggil di btnTerimaPO, dalam transaksi yang sama).
        /// Debit Persediaan [Total]; Kredit Kas [DP]; Kredit Hutang Usaha
        /// [Total - DP kalau &gt; 0]; Debit Uang Muka Pembelian [DP - Total
        /// kalau DP &gt; Total] — supaya tetap seimbang saat DP melebihi
        /// total PO.
        /// </summary>
        public static long TulisJurnalPembelian(SQLiteConnection conn,
            SQLiteTransaction trans, string tanggal, string noPO, long idPO,
            decimal total, decimal dp)
        {
            return TulisJurnal(conn, trans, tanggal, noPO,
                "Terima barang PO", "Pembelian", idPO,
                new List<BarisJurnal>
                {
                    new BarisJurnal("1103", total, 0),
                    new BarisJurnal("1101", 0, dp),
                    new BarisJurnal("2101", 0, total > dp ? total - dp : 0),
                    new BarisJurnal("1104", dp > total ? dp - total : 0, 0)
                });
        }

        /// <summary>
        /// Bayar Hutang (dipanggil di btnBayarHutang, dalam transaksi yang
        /// sama). Hutang ke supplier titipan (RefType 'KonsinyasiMasuk')
        /// memakai akun 2102 Hutang Konsinyasi, yang lain 2101 Hutang
        /// Usaha. Debit Hutang [jumlah]; Kredit Kas [jumlah].
        /// </summary>
        public static long TulisJurnalBayarHutang(SQLiteConnection conn,
            SQLiteTransaction trans, string tanggal, decimal jumlah,
            string refTypeHutang, long idHutang)
        {
            string kodeHutang = refTypeHutang == "KonsinyasiMasuk" ? "2102" : "2101";
            return TulisJurnal(conn, trans, tanggal, "HB-" + idHutang,
                "Bayar hutang", "BayarHutang", idHutang,
                new List<BarisJurnal>
                {
                    new BarisJurnal(kodeHutang, jumlah, 0),
                    new BarisJurnal("1101", 0, jumlah)
                });
        }

        /// <summary>
        /// Terima Piutang (dipanggil di btnBayarPiutang, dalam transaksi
        /// yang sama). Debit Kas [jumlah]; Kredit Piutang Usaha [jumlah].
        /// </summary>
        public static long TulisJurnalTerimaPiutang(SQLiteConnection conn,
            SQLiteTransaction trans, string tanggal, decimal jumlah,
            long idPiutang)
        {
            return TulisJurnal(conn, trans, tanggal, "TP-" + idPiutang,
                "Terima piutang", "TerimaPiutang", idPiutang,
                new List<BarisJurnal>
                {
                    new BarisJurnal("1101", jumlah, 0),
                    new BarisJurnal("1102", 0, jumlah)
                });
        }

        /// <summary>
        /// Jual titipan Konsinyasi Masuk (dipanggil di btnJualKSM, dalam
        /// transaksi yang sama). Debit Kas [totalJual]; Kredit Pendapatan
        /// Penjualan [totalJual]; Debit HPP [totalTitipan]; Kredit Hutang
        /// Konsinyasi [totalTitipan] — barang milik supplier, jadi HPP-nya
        /// lawan hutang ke supplier, bukan persediaan toko.
        /// </summary>
        public static long TulisJurnalJualKonsinyasiMasuk(SQLiteConnection conn,
            SQLiteTransaction trans, string tanggal, long idTransaksi,
            decimal totalJual, decimal totalTitipan)
        {
            return TulisJurnal(conn, trans, tanggal, "INV-" + idTransaksi,
                "Penjualan titipan konsinyasi masuk", "JualKonsinyasiMasuk",
                idTransaksi,
                new List<BarisJurnal>
                {
                    new BarisJurnal("1101", totalJual, 0),
                    new BarisJurnal("4101", 0, totalJual),
                    new BarisJurnal("5101", totalTitipan, 0),
                    new BarisJurnal("2102", 0, totalTitipan)
                });
        }

        /// <summary>
        /// Jual agen Konsinyasi Keluar (dipanggil di btnJualKSK, dalam
        /// transaksi yang sama). Debit Piutang Usaha [totalJual]; Kredit
        /// Pendapatan Penjualan [totalJual]; Debit HPP [totalHPP]; Kredit
        /// Persediaan [totalHPP] — stok sudah keluar saat dititipkan.
        /// </summary>
        public static long TulisJurnalJualKonsinyasiKeluar(SQLiteConnection conn,
            SQLiteTransaction trans, string tanggal, long idTransaksi,
            decimal totalJual, decimal totalHPP)
        {
            return TulisJurnal(conn, trans, tanggal, "INV-" + idTransaksi,
                "Penjualan agen konsinyasi keluar", "JualKonsinyasiKeluar",
                idTransaksi,
                new List<BarisJurnal>
                {
                    new BarisJurnal("1102", totalJual, 0),
                    new BarisJurnal("4101", 0, totalJual),
                    new BarisJurnal("5101", totalHPP, 0),
                    new BarisJurnal("1103", 0, totalHPP)
                });
        }

        /// <summary>
        /// Stok Opname selesai (dipanggil di btnSelesaikanOpname, dalam
        /// transaksi yang sama). Surplus: Debit Persediaan [nilaiSurplus];
        /// Kredit Pendapatan Lain-lain [nilaiSurplus]. Shortfall (nilai
        /// HPP dari batch yang terpakai): Debit Biaya Selisih Persediaan
        /// [nilaiShortfall]; Kredit Persediaan [nilaiShortfall]. Kalau
        /// keduanya 0, tidak ada jurnal yang dibuat (return 0).
        /// </summary>
        public static long TulisJurnalOpname(SQLiteConnection conn,
            SQLiteTransaction trans, string tanggal, long idOpname,
            decimal nilaiSurplus, decimal nilaiShortfall)
        {
            return TulisJurnal(conn, trans, tanggal, "OPN-" + idOpname,
                "Penyesuaian stok opname", "Opname", idOpname,
                new List<BarisJurnal>
                {
                    new BarisJurnal("1103", nilaiSurplus, 0),
                    new BarisJurnal("4102", 0, nilaiSurplus),
                    new BarisJurnal("6106", nilaiShortfall, 0),
                    new BarisJurnal("1103", 0, nilaiShortfall)
                });
        }

        /// <summary>
        /// Biaya operasional manual (dipanggil saat tambah biaya, dalam
        /// transaksi yang sama). Debit akun biaya [jumlah]; Kredit Kas
        /// [jumlah]. kodeAkunBiaya harus akun Jenis 'Biaya' (6101-6106).
        /// </summary>
        public static long TulisJurnalBiaya(SQLiteConnection conn,
            SQLiteTransaction trans, string tanggal, string kodeAkunBiaya,
            decimal jumlah, long idBiaya)
        {
            return TulisJurnal(conn, trans, tanggal, "BIA-" + idBiaya,
                "Biaya operasional", "Biaya", idBiaya,
                new List<BarisJurnal>
                {
                    new BarisJurnal(kodeAkunBiaya, jumlah, 0),
                    new BarisJurnal("1101", 0, jumlah)
                });
        }

        /// <summary>
        /// Terima Deposit (dipanggil saat terima deposit, dalam transaksi
        /// yang sama). Debit Kas [jumlah]; Kredit Deposit Pelanggan
        /// [jumlah] — deposit adalah kewajiban, bukan pendapatan.
        /// </summary>
        public static long TulisJurnalDepositTerima(SQLiteConnection conn,
            SQLiteTransaction trans, string tanggal, decimal jumlah,
            long idDeposit)
        {
            return TulisJurnal(conn, trans, tanggal, "DEP-" + idDeposit,
                "Terima deposit pelanggan", "DepositTerima", idDeposit,
                new List<BarisJurnal>
                {
                    new BarisJurnal("1101", jumlah, 0),
                    new BarisJurnal("2103", 0, jumlah)
                });
        }

        /// <summary>
        /// Kembalikan Deposit (dipanggil saat refund deposit, dalam
        /// transaksi yang sama). Debit Deposit Pelanggan [jumlah]; Kredit
        /// Kas [jumlah].
        /// </summary>
        public static long TulisJurnalDepositKembali(SQLiteConnection conn,
            SQLiteTransaction trans, string tanggal, decimal jumlah,
            long idDeposit)
        {
            return TulisJurnal(conn, trans, tanggal, "DEP-" + idDeposit,
                "Kembalikan deposit pelanggan", "DepositKembali", idDeposit,
                new List<BarisJurnal>
                {
                    new BarisJurnal("2103", jumlah, 0),
                    new BarisJurnal("1101", 0, jumlah)
                });
        }

        /// <summary>
        /// Saldo satu akun (berdasarkan Kode) sampai tanggalAkhir
        /// (inklusif, format 'yyyy-MM-dd HH:mm:ss'): SaldoAwal + gerakan
        /// jurnal bersih sesuai SaldoNormal (Debit: Debit - Kredit;
        /// Kredit: Kredit - Debit). Dipakai Neraca &amp; Buku Besar.
        /// </summary>
        public static decimal SaldoAkunSampai(SQLiteConnection conn,
            SQLiteTransaction trans, string kode, string tanggalAkhir)
        {
            string saldoNormal;
            decimal saldoAwal;
            long idAkun = IdAkun(conn, trans, kode);
            using (var cmd = BuatCmd(conn, trans,
                "SELECT SaldoNormal, SaldoAwal FROM Akun WHERE ID_Akun = @ID"))
            {
                cmd.Parameters.AddWithValue("@ID", idAkun);
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return 0;
                    saldoNormal = Convert.ToString(reader["SaldoNormal"]);
                    saldoAwal = Convert.ToDecimal(reader["SaldoAwal"]);
                }
            }
            decimal debit = 0;
            decimal kredit = 0;
            using (var cmd = BuatCmd(conn, trans,
                "SELECT COALESCE(SUM(d.Debit), 0), COALESCE(SUM(d.Kredit), 0) " +
                "FROM JurnalDetail d INNER JOIN Jurnal j " +
                "ON j.ID_Jurnal = d.ID_Jurnal " +
                "WHERE d.ID_Akun = @ID AND j.Tanggal <= @Akhir"))
            {
                cmd.Parameters.AddWithValue("@ID", idAkun);
                cmd.Parameters.AddWithValue("@Akhir", tanggalAkhir);
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        debit = Convert.ToDecimal(reader[0]);
                        kredit = Convert.ToDecimal(reader[1]);
                    }
                }
            }
            return saldoNormal == "Kredit"
                ? saldoAwal + kredit - debit
                : saldoAwal + debit - kredit;
        }

        /// <summary>
        /// Gerakan bersih satu akun (berdasarkan Kode) dalam rentang
        /// [tanggalAwal, tanggalAkhir] (inklusif), ditandatangani sesuai
        /// SaldoNormal: akun Debit-normal positif kalau Debit &gt; Kredit
        /// (mis. HPP naik), akun Kredit-normal positif kalau Kredit &gt;
        /// Debit (mis. Pendapatan naik). Dipakai Laba Rugi &amp; Laba
        /// Berjalan di Neraca.
        /// </summary>
        public static decimal GerakanAkunPeriode(SQLiteConnection conn,
            SQLiteTransaction trans, string kode, string tanggalAwal,
            string tanggalAkhir)
        {
            string saldoNormal;
            long idAkun = IdAkun(conn, trans, kode);
            using (var cmd = BuatCmd(conn, trans,
                "SELECT SaldoNormal FROM Akun WHERE ID_Akun = @ID"))
            {
                cmd.Parameters.AddWithValue("@ID", idAkun);
                saldoNormal = Convert.ToString(cmd.ExecuteScalar());
            }
            decimal debit = 0;
            decimal kredit = 0;
            using (var cmd = BuatCmd(conn, trans,
                "SELECT COALESCE(SUM(d.Debit), 0), COALESCE(SUM(d.Kredit), 0) " +
                "FROM JurnalDetail d INNER JOIN Jurnal j " +
                "ON j.ID_Jurnal = d.ID_Jurnal " +
                "WHERE d.ID_Akun = @ID AND j.Tanggal >= @Awal AND j.Tanggal <= @Akhir"))
            {
                cmd.Parameters.AddWithValue("@ID", idAkun);
                cmd.Parameters.AddWithValue("@Awal", tanggalAwal);
                cmd.Parameters.AddWithValue("@Akhir", tanggalAkhir);
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        debit = Convert.ToDecimal(reader[0]);
                        kredit = Convert.ToDecimal(reader[1]);
                    }
                }
            }
            return saldoNormal == "Kredit" ? kredit - debit : debit - kredit;
        }
    }
}
