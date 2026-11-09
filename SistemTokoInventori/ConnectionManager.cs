using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace SistemTokoInventori
{
    /// <summary>
    /// Fase 2 (v2) — SATU database: TokoBangunan.db di folder aplikasi
    /// (samping file .exe, AppDomain.CurrentDomain.BaseDirectory).
    ///
    /// Menggantikan perilaku lama (3 file .db di folder aplikasi:
    /// DB_Produk.db, DB_Transaksi.db, DB_Supplier.db). File-file lama
    /// TIDAK PERNAH dihapus — pada first run, kalau database baru masih
    /// kosong, isinya diimpor satu kali (lihat MigrateOldDatabases).
    ///
    /// File TokoBangunan.db yang sudah jadi (39 tabel + seed) dibundel
    /// di proyek dan disalin ke output lewat .csproj (Content,
    /// CopyToOutputDirectory = PreserveNewest) supaya langsung terlihat
    /// di samping .exe. Kalau file dihapus, EnsureDatabase membuatnya
    /// ulang dari skema + seed di bawah ini.
    ///
    /// B12 dipertahankan: Busy Timeout=5000 di connection string.
    /// </summary>
    public static class ConnectionManager
    {
        // Folder .exe (bin\Debug) — tempat file TokoBangunan.db berada,
        // sekaligus tempat file .db LAMA berada (untuk migrasi).
        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;

        private static readonly string DbPath = Path.Combine(BaseDir, "TokoBangunan.db");

        /// <summary>Path lengkap database aktif (berguna untuk diagnosis).</summary>
        public static string DatabasePath { get { return DbPath; } }

        // ===== Skema v2 — 39 tabel dalam satu database =====
        // 8 tabel Fase 1 + 9 tabel Fase 2 (Pembelian + Penjualan lanjutan)
        // + 16 tabel Fase 3 (Konsinyasi + Gudang) + 6 tabel Fase 4
        // (Keuangan & Akuntansi).
        // CREATE TABLE IF NOT EXISTS: database yang dibuat di Fase 1 cukup
        // menjalankan EnsureDatabase lagi — tabel Fase 2/3/4 dibuat otomatis.
        private const string ScriptSchema = @"
CREATE TABLE IF NOT EXISTS Supplier (
    ID_Supplier INTEGER PRIMARY KEY AUTOINCREMENT,
    Nama_Supplier TEXT NOT NULL,
    Kontak TEXT,
    Alamat TEXT
);
CREATE TABLE IF NOT EXISTS Item (
    ID_Item INTEGER PRIMARY KEY AUTOINCREMENT,
    Kode TEXT UNIQUE,
    Nama TEXT NOT NULL,
    Tipe TEXT NOT NULL DEFAULT 'Fisik' CHECK (Tipe IN ('Fisik','Rakitan','NonInventori','Jasa')),
    HargaBeli REAL NOT NULL DEFAULT 0,
    HargaJual REAL NOT NULL DEFAULT 0,
    Stok REAL NOT NULL DEFAULT 0,
    MinStok REAL NOT NULL DEFAULT 0,
    Satuan TEXT,
    ID_Supplier INTEGER,
    Barcode TEXT,
    FOREIGN KEY (ID_Supplier) REFERENCES Supplier(ID_Supplier)
);
CREATE TABLE IF NOT EXISTS Pelanggan (
    ID_Pelanggan INTEGER PRIMARY KEY AUTOINCREMENT,
    Nama TEXT NOT NULL,
    Telepon TEXT,
    Alamat TEXT,
    Poin INTEGER NOT NULL DEFAULT 0
);
CREATE TABLE IF NOT EXISTS DiskonPeriode (
    ID_Diskon INTEGER PRIMARY KEY AUTOINCREMENT,
    Nama TEXT NOT NULL,
    Persen REAL NOT NULL DEFAULT 0,
    TglMulai TEXT,
    TglSelesai TEXT,
    Aktif INTEGER NOT NULL DEFAULT 1
);
CREATE TABLE IF NOT EXISTS Transaksi (
    ID_Transaksi INTEGER PRIMARY KEY AUTOINCREMENT,
    Tanggal TEXT NOT NULL,
    Total REAL NOT NULL,
    Bayar REAL NOT NULL DEFAULT 0,
    Kembalian REAL NOT NULL DEFAULT 0,
    ID_Pelanggan INTEGER NULL,
    Diskon REAL NOT NULL DEFAULT 0,
    PoinDipakai INTEGER NOT NULL DEFAULT 0,
    PoinDidapat INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (ID_Pelanggan) REFERENCES Pelanggan(ID_Pelanggan)
);
CREATE TABLE IF NOT EXISTS Detail_Transaksi (
    ID_Detail INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_Transaksi INTEGER NOT NULL,
    ID_Item INTEGER NOT NULL,
    Qty REAL NOT NULL,
    HargaSatuan REAL NOT NULL,
    DiskonItem REAL NOT NULL DEFAULT 0,
    Subtotal REAL NOT NULL,
    FOREIGN KEY (ID_Transaksi) REFERENCES Transaksi(ID_Transaksi),
    FOREIGN KEY (ID_Item) REFERENCES Item(ID_Item)
);
CREATE TABLE IF NOT EXISTS HargaJualHistory (
    ID_History INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_Item INTEGER NOT NULL,
    HargaLama REAL NOT NULL,
    HargaBaru REAL NOT NULL,
    Tanggal TEXT NOT NULL,
    Keterangan TEXT,
    FOREIGN KEY (ID_Item) REFERENCES Item(ID_Item)
);
CREATE TABLE IF NOT EXISTS StokMovement (
    ID_Movement INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_Item INTEGER NOT NULL,
    Tanggal TEXT NOT NULL,
    Jenis TEXT NOT NULL,
    Qty REAL NOT NULL,
    StokSesudah REAL NOT NULL,
    RefID TEXT,
    Keterangan TEXT,
    FOREIGN KEY (ID_Item) REFERENCES Item(ID_Item)
);";

        // ===== Skema Fase 2 — Pembelian + Penjualan lanjutan =====
        // 9 tabel baru. Penomoran: NoPO = 'PO-' + yyyy + '-' + 4 digit
        // urutan tahun itu; NoSO = 'SO-' + yyyy + '-' + 4 digit;
        // NoFaktur piutang = 'INV-' + ID_Transaksi (nomor transaksi yang
        // sama dengan yang tampil di struk).
        private const string ScriptSchemaFase2 = @"
CREATE TABLE IF NOT EXISTS PurchaseOrder (
    ID_PO INTEGER PRIMARY KEY AUTOINCREMENT,
    NoPO TEXT UNIQUE,
    ID_Supplier INTEGER,
    Tanggal TEXT,
    Status TEXT NOT NULL DEFAULT 'Draft' CHECK (Status IN ('Draft','Dikirim','Diterima','Dibatalkan')),
    Total REAL NOT NULL DEFAULT 0,
    DP REAL NOT NULL DEFAULT 0,
    Catatan TEXT,
    FOREIGN KEY (ID_Supplier) REFERENCES Supplier(ID_Supplier)
);
CREATE TABLE IF NOT EXISTS PODetail (
    ID_PODetail INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_PO INTEGER NOT NULL,
    ID_Item INTEGER NOT NULL,
    Qty REAL NOT NULL,
    HargaBeli REAL NOT NULL,
    Subtotal REAL NOT NULL,
    FOREIGN KEY (ID_PO) REFERENCES PurchaseOrder(ID_PO),
    FOREIGN KEY (ID_Item) REFERENCES Item(ID_Item)
);
CREATE TABLE IF NOT EXISTS Hutang (
    ID_Hutang INTEGER PRIMARY KEY AUTOINCREMENT,
    RefType TEXT,
    RefID INTEGER,
    ID_Supplier INTEGER,
    Tanggal TEXT,
    JatuhTempo TEXT,
    Total REAL NOT NULL DEFAULT 0,
    Sisa REAL NOT NULL DEFAULT 0,
    Status TEXT NOT NULL DEFAULT 'Belum Lunas' CHECK (Status IN ('Belum Lunas','Lunas')),
    FOREIGN KEY (ID_Supplier) REFERENCES Supplier(ID_Supplier)
);
CREATE TABLE IF NOT EXISTS HutangBayar (
    ID_HutangBayar INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_Hutang INTEGER NOT NULL,
    Tanggal TEXT,
    Jumlah REAL NOT NULL,
    Keterangan TEXT,
    FOREIGN KEY (ID_Hutang) REFERENCES Hutang(ID_Hutang)
);
CREATE TABLE IF NOT EXISTS SalesOrder (
    ID_SO INTEGER PRIMARY KEY AUTOINCREMENT,
    NoSO TEXT UNIQUE,
    ID_Pelanggan INTEGER,
    Tanggal TEXT,
    Status TEXT NOT NULL DEFAULT 'Draft' CHECK (Status IN ('Draft','Diproses','Selesai','Dibatalkan')),
    Total REAL NOT NULL DEFAULT 0,
    Catatan TEXT,
    FOREIGN KEY (ID_Pelanggan) REFERENCES Pelanggan(ID_Pelanggan)
);
CREATE TABLE IF NOT EXISTS SODetail (
    ID_SODetail INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_SO INTEGER NOT NULL,
    ID_Item INTEGER NOT NULL,
    Qty REAL NOT NULL,
    HargaJual REAL NOT NULL,
    DiskonItem REAL NOT NULL DEFAULT 0,
    Subtotal REAL NOT NULL,
    FOREIGN KEY (ID_SO) REFERENCES SalesOrder(ID_SO),
    FOREIGN KEY (ID_Item) REFERENCES Item(ID_Item)
);
CREATE TABLE IF NOT EXISTS Piutang (
    ID_Piutang INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_Transaksi INTEGER NULL,
    NoFaktur TEXT,
    ID_Pelanggan INTEGER,
    Tanggal TEXT,
    JatuhTempo TEXT,
    Total REAL NOT NULL DEFAULT 0,
    Sisa REAL NOT NULL DEFAULT 0,
    Status TEXT NOT NULL DEFAULT 'Belum Lunas' CHECK (Status IN ('Belum Lunas','Lunas')),
    FOREIGN KEY (ID_Transaksi) REFERENCES Transaksi(ID_Transaksi),
    FOREIGN KEY (ID_Pelanggan) REFERENCES Pelanggan(ID_Pelanggan)
);
CREATE TABLE IF NOT EXISTS PiutangBayar (
    ID_PiutangBayar INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_Piutang INTEGER NOT NULL,
    Tanggal TEXT,
    Jumlah REAL NOT NULL,
    Keterangan TEXT,
    FOREIGN KEY (ID_Piutang) REFERENCES Piutang(ID_Piutang)
);
CREATE TABLE IF NOT EXISTS TukarTambah (
    ID_TukarTambah INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_Transaksi INTEGER NOT NULL,
    DeskripsiBarang TEXT,
    ID_ItemMasuk INTEGER NULL,
    QtyMasuk REAL NOT NULL DEFAULT 0,
    NilaiTukar REAL NOT NULL,
    Keterangan TEXT,
    FOREIGN KEY (ID_Transaksi) REFERENCES Transaksi(ID_Transaksi),
    FOREIGN KEY (ID_ItemMasuk) REFERENCES Item(ID_Item)
);";

        // ===== Skema Fase 3 — Konsinyasi + Persediaan & Gudang =====
        // 16 tabel baru. Lokasi + StokLokasi: stok per gudang (Item.Stok
        // adalah total semua lokasi). StokBatch: batch pembelian untuk
        // HPP FIFO/LIFO (metode disimpan di Pengaturan, kunci HPPMethod).
        // KonsinyasiMasuk/Keluar (+Detail +Jual): titipan supplier/agen.
        // Rakitan (+Detail): resep perakitan. TransferStok (+Detail):
        // mutasi antar lokasi. Opname (+Detail): stok opname per lokasi.
        // Penomoran: NoKSM/NoKSK/NoTransfer/NoOpname = prefix + yyyy +
        // '-' + 4 digit urutan tahun itu (dihitung dari COUNT(*)).
        private const string ScriptSchemaFase3 = @"
CREATE TABLE IF NOT EXISTS Lokasi (
    ID_Lokasi INTEGER PRIMARY KEY AUTOINCREMENT,
    Kode TEXT UNIQUE,
    Nama TEXT NOT NULL,
    Alamat TEXT,
    IsAktif INTEGER NOT NULL DEFAULT 1
);
CREATE TABLE IF NOT EXISTS StokLokasi (
    ID_Lokasi INTEGER NOT NULL,
    ID_Item INTEGER NOT NULL,
    Qty REAL NOT NULL DEFAULT 0,
    PRIMARY KEY (ID_Lokasi, ID_Item),
    FOREIGN KEY (ID_Lokasi) REFERENCES Lokasi(ID_Lokasi),
    FOREIGN KEY (ID_Item) REFERENCES Item(ID_Item)
);
CREATE TABLE IF NOT EXISTS StokBatch (
    ID_Batch INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_Item INTEGER NOT NULL,
    ID_Lokasi INTEGER NOT NULL,
    TanggalMasuk TEXT NOT NULL,
    QtyAwal REAL NOT NULL,
    QtySisa REAL NOT NULL,
    HargaBeli REAL NOT NULL,
    RefType TEXT,
    RefID INTEGER,
    FOREIGN KEY (ID_Item) REFERENCES Item(ID_Item),
    FOREIGN KEY (ID_Lokasi) REFERENCES Lokasi(ID_Lokasi)
);
CREATE TABLE IF NOT EXISTS Pengaturan (
    Kunci TEXT PRIMARY KEY,
    Nilai TEXT
);
CREATE TABLE IF NOT EXISTS KonsinyasiMasuk (
    ID_KSM INTEGER PRIMARY KEY AUTOINCREMENT,
    NoKSM TEXT UNIQUE,
    ID_Supplier INTEGER,
    Tanggal TEXT,
    Status TEXT NOT NULL DEFAULT 'Aktif' CHECK (Status IN ('Aktif','Selesai','Dibatalkan')),
    Catatan TEXT,
    FOREIGN KEY (ID_Supplier) REFERENCES Supplier(ID_Supplier)
);
CREATE TABLE IF NOT EXISTS KonsinyasiMasukDetail (
    ID_KSMDetail INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_KSM INTEGER NOT NULL,
    ID_Item INTEGER NOT NULL,
    QtyTitipan REAL NOT NULL,
    QtyTerjual REAL NOT NULL DEFAULT 0,
    HargaTitipan REAL NOT NULL,
    HargaJual REAL NOT NULL,
    FOREIGN KEY (ID_KSM) REFERENCES KonsinyasiMasuk(ID_KSM),
    FOREIGN KEY (ID_Item) REFERENCES Item(ID_Item)
);
CREATE TABLE IF NOT EXISTS KonsinyasiMasukJual (
    ID_KMSJ INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_KSMDetail INTEGER NOT NULL,
    Tanggal TEXT,
    Qty REAL NOT NULL,
    HargaJual REAL NOT NULL,
    ID_Transaksi INTEGER NULL,
    FOREIGN KEY (ID_KSMDetail) REFERENCES KonsinyasiMasukDetail(ID_KSMDetail),
    FOREIGN KEY (ID_Transaksi) REFERENCES Transaksi(ID_Transaksi)
);
CREATE TABLE IF NOT EXISTS KonsinyasiKeluar (
    ID_KSK INTEGER PRIMARY KEY AUTOINCREMENT,
    NoKSK TEXT UNIQUE,
    ID_Pelanggan INTEGER,
    Tanggal TEXT,
    Status TEXT NOT NULL DEFAULT 'Aktif' CHECK (Status IN ('Aktif','Selesai','Dibatalkan')),
    Catatan TEXT,
    FOREIGN KEY (ID_Pelanggan) REFERENCES Pelanggan(ID_Pelanggan)
);
CREATE TABLE IF NOT EXISTS KonsinyasiKeluarDetail (
    ID_KSKDetail INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_KSK INTEGER NOT NULL,
    ID_Item INTEGER NOT NULL,
    QtyTitip REAL NOT NULL,
    QtyTerjual REAL NOT NULL DEFAULT 0,
    HargaJual REAL NOT NULL,
    HPPUnit REAL NOT NULL DEFAULT 0,
    FOREIGN KEY (ID_KSK) REFERENCES KonsinyasiKeluar(ID_KSK),
    FOREIGN KEY (ID_Item) REFERENCES Item(ID_Item)
);
CREATE TABLE IF NOT EXISTS KonsinyasiKeluarJual (
    ID_KSKJ INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_KSKDetail INTEGER NOT NULL,
    Tanggal TEXT,
    Qty REAL NOT NULL,
    HargaJual REAL NOT NULL,
    ID_Transaksi INTEGER NULL,
    FOREIGN KEY (ID_KSKDetail) REFERENCES KonsinyasiKeluarDetail(ID_KSKDetail),
    FOREIGN KEY (ID_Transaksi) REFERENCES Transaksi(ID_Transaksi)
);
CREATE TABLE IF NOT EXISTS Rakitan (
    ID_Rakitan INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_ItemJadi INTEGER NOT NULL,
    Nama TEXT,
    Aktif INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (ID_ItemJadi) REFERENCES Item(ID_Item)
);
CREATE TABLE IF NOT EXISTS RakitanDetail (
    ID_RakitanDetail INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_Rakitan INTEGER NOT NULL,
    ID_ItemBahan INTEGER NOT NULL,
    Qty REAL NOT NULL,
    FOREIGN KEY (ID_Rakitan) REFERENCES Rakitan(ID_Rakitan),
    FOREIGN KEY (ID_ItemBahan) REFERENCES Item(ID_Item)
);
CREATE TABLE IF NOT EXISTS TransferStok (
    ID_Transfer INTEGER PRIMARY KEY AUTOINCREMENT,
    NoTransfer TEXT UNIQUE,
    ID_LokasiAsal INTEGER NOT NULL,
    ID_LokasiTujuan INTEGER NOT NULL,
    Tanggal TEXT,
    Status TEXT NOT NULL DEFAULT 'Draft' CHECK (Status IN ('Draft','Selesai','Dibatalkan')),
    Catatan TEXT,
    FOREIGN KEY (ID_LokasiAsal) REFERENCES Lokasi(ID_Lokasi),
    FOREIGN KEY (ID_LokasiTujuan) REFERENCES Lokasi(ID_Lokasi)
);
CREATE TABLE IF NOT EXISTS TransferDetail (
    ID_TransferDetail INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_Transfer INTEGER NOT NULL,
    ID_Item INTEGER NOT NULL,
    Qty REAL NOT NULL,
    FOREIGN KEY (ID_Transfer) REFERENCES TransferStok(ID_Transfer),
    FOREIGN KEY (ID_Item) REFERENCES Item(ID_Item)
);
CREATE TABLE IF NOT EXISTS Opname (
    ID_Opname INTEGER PRIMARY KEY AUTOINCREMENT,
    NoOpname TEXT UNIQUE,
    ID_Lokasi INTEGER NOT NULL,
    Tanggal TEXT,
    Status TEXT NOT NULL DEFAULT 'Draft' CHECK (Status IN ('Draft','Selesai')),
    Catatan TEXT,
    FOREIGN KEY (ID_Lokasi) REFERENCES Lokasi(ID_Lokasi)
);
CREATE TABLE IF NOT EXISTS OpnameDetail (
    ID_OpnameDetail INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_Opname INTEGER NOT NULL,
    ID_Item INTEGER NOT NULL,
    StokSistem REAL NOT NULL,
    StokFisik REAL NOT NULL,
    Selisih REAL NOT NULL,
    FOREIGN KEY (ID_Opname) REFERENCES Opname(ID_Opname),
    FOREIGN KEY (ID_Item) REFERENCES Item(ID_Item)
);";

        // ===== Skema Fase 4 — Keuangan & Akuntansi =====
        // 6 tabel baru. Akun: bagan akun (chart of accounts) dengan jenis
        // + saldo normal; SaldoAwal diisi lewat editor di tab Keuangan
        // (bukan dari transaksi — transaksi Fase 1-3 TIDAK di-backfill
        // ke jurnal, lihat §10 V2_NOTES). Jurnal + JurnalDetail: jurnal
        // otomatis yang ditulis JurnalHelper di dalam transaksi yang sama
        // dengan operasi bisnisnya; setiap jurnal harus seimbang
        // (SUM Debit == SUM Kredit) atau helper melempar. Deposit:
        // uang muka pelanggan (kewajiban, bukan pendapatan) + pemakaian
        // FIFO di Kasir + pengembalian. BiayaOperasional: biaya manual
        // (gaji, listrik, sewa, dll.) yang juga menjurnal ke Kas.
        private const string ScriptSchemaFase4 = @"
CREATE TABLE IF NOT EXISTS Akun (
    ID_Akun INTEGER PRIMARY KEY AUTOINCREMENT,
    Kode TEXT UNIQUE,
    Nama TEXT NOT NULL,
    Jenis TEXT NOT NULL CHECK (Jenis IN ('Aktiva','Kewajiban','Modal','Pendapatan','HPP','Biaya')),
    SaldoNormal TEXT NOT NULL CHECK (SaldoNormal IN ('Debit','Kredit')),
    SaldoAwal REAL NOT NULL DEFAULT 0,
    IsAktif INTEGER NOT NULL DEFAULT 1
);
CREATE TABLE IF NOT EXISTS Jurnal (
    ID_Jurnal INTEGER PRIMARY KEY AUTOINCREMENT,
    Tanggal TEXT NOT NULL,
    NoBukti TEXT,
    Keterangan TEXT,
    RefType TEXT,
    RefID INTEGER
);
CREATE TABLE IF NOT EXISTS JurnalDetail (
    ID_JurnalDetail INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_Jurnal INTEGER NOT NULL,
    ID_Akun INTEGER NOT NULL,
    Debit REAL NOT NULL DEFAULT 0,
    Kredit REAL NOT NULL DEFAULT 0,
    FOREIGN KEY (ID_Jurnal) REFERENCES Jurnal(ID_Jurnal),
    FOREIGN KEY (ID_Akun) REFERENCES Akun(ID_Akun)
);
CREATE TABLE IF NOT EXISTS Deposit (
    ID_Deposit INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_Pelanggan INTEGER NOT NULL,
    Tanggal TEXT NOT NULL,
    Jumlah REAL NOT NULL,
    Sisa REAL NOT NULL,
    Status TEXT NOT NULL DEFAULT 'Aktif' CHECK (Status IN ('Aktif','Terpakai','Dikembalikan')),
    Keterangan TEXT,
    FOREIGN KEY (ID_Pelanggan) REFERENCES Pelanggan(ID_Pelanggan)
);
CREATE TABLE IF NOT EXISTS DepositPakai (
    ID_DepositPakai INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_Deposit INTEGER NOT NULL,
    ID_Transaksi INTEGER NOT NULL,
    Tanggal TEXT NOT NULL,
    Jumlah REAL NOT NULL,
    FOREIGN KEY (ID_Deposit) REFERENCES Deposit(ID_Deposit),
    FOREIGN KEY (ID_Transaksi) REFERENCES Transaksi(ID_Transaksi)
);
CREATE TABLE IF NOT EXISTS BiayaOperasional (
    ID_Biaya INTEGER PRIMARY KEY AUTOINCREMENT,
    Tanggal TEXT NOT NULL,
    ID_Akun INTEGER NOT NULL,
    Jumlah REAL NOT NULL,
    Keterangan TEXT,
    NoBukti TEXT,
    FOREIGN KEY (ID_Akun) REFERENCES Akun(ID_Akun)
);";

        // Bagan akun awal — INSERT OR IGNORE (Kode UNIQUE) supaya
        // idempotent. 1104 'Uang Muka Pembelian' menampung kelebihan DP
        // PO (DP > Total) supaya jurnal terima PO tetap seimbang.
        private const string ScriptSeedAkun = @"
INSERT OR IGNORE INTO Akun (Kode, Nama, Jenis, SaldoNormal) VALUES
('1101', 'Kas', 'Aktiva', 'Debit'),
('1102', 'Piutang Usaha', 'Aktiva', 'Debit'),
('1103', 'Persediaan', 'Aktiva', 'Debit'),
('1104', 'Uang Muka Pembelian', 'Aktiva', 'Debit'),
('1201', 'Aset Tetap', 'Aktiva', 'Debit'),
('2101', 'Hutang Usaha', 'Kewajiban', 'Kredit'),
('2102', 'Hutang Konsinyasi', 'Kewajiban', 'Kredit'),
('2103', 'Deposit Pelanggan', 'Kewajiban', 'Kredit'),
('3101', 'Modal Pemilik', 'Modal', 'Kredit'),
('3201', 'Laba Ditahan', 'Modal', 'Kredit'),
('4101', 'Pendapatan Penjualan', 'Pendapatan', 'Kredit'),
('4102', 'Pendapatan Lain-lain', 'Pendapatan', 'Kredit'),
('5101', 'Harga Pokok Penjualan', 'HPP', 'Debit'),
('6101', 'Biaya Gaji', 'Biaya', 'Debit'),
('6102', 'Biaya Listrik & Air', 'Biaya', 'Debit'),
('6103', 'Biaya Sewa', 'Biaya', 'Debit'),
('6104', 'Biaya Lain-lain', 'Biaya', 'Debit'),
('6105', 'Biaya Promosi & Poin', 'Biaya', 'Debit'),
('6106', 'Biaya Selisih Persediaan', 'Biaya', 'Debit');";

        // Data contoh — hanya diisi kalau tabelnya masih kosong setelah
        // migrasi (database benar-benar baru, tanpa file lama untuk diimpor).
        // ID ditulis eksplisit supaya detail transaksi contoh konsisten.
        private const string ScriptSeedSupplier = @"
INSERT INTO Supplier (ID_Supplier, Nama_Supplier, Kontak, Alamat) VALUES
(1, 'PT Indofood', '021-12345678', 'Jakarta Pusat'),
(2, 'PT Aqua Danone', '021-87654321', 'Bekasi'),
(3, 'PT Mayora', '021-11223344', 'Tangerang');";

        private const string ScriptSeedItem = @"
INSERT INTO Item (ID_Item, Kode, Nama, Tipe, HargaBeli, HargaJual, Stok, MinStok, Satuan, ID_Supplier, Barcode) VALUES
(1, 'BRG-0001', 'Indomie Goreng', 'Fisik', 3000, 3500, 100, 10, 'pcs', 1, NULL),
(2, 'BRG-0002', 'Aqua 600ml', 'Fisik', 2500, 3000, 200, 10, 'pcs', 2, NULL),
(3, 'BRG-0003', 'Susu Ultra 1L', 'Fisik', 16000, 18000, 50, 10, 'pcs', 1, NULL),
(4, 'BRG-0004', 'Teh Pucuk 350ml', 'Fisik', 3500, 4000, 150, 10, 'pcs', 3, NULL);";

        private const string ScriptSeedTransaksi = @"
INSERT INTO Transaksi (ID_Transaksi, Tanggal, Total, Bayar, Kembalian) VALUES
(1, '2026-10-20 10:30:00', 21000, 21000, 0),
(2, '2026-10-21 14:15:00', 32000, 50000, 18000);
INSERT INTO Detail_Transaksi (ID_Detail, ID_Transaksi, ID_Item, Qty, HargaSatuan, DiskonItem, Subtotal) VALUES
(1, 1, 1, 6, 3500, 0, 21000),
(2, 2, 2, 4, 3000, 0, 12000),
(3, 2, 4, 5, 4000, 0, 20000);";

        private static string GetConnString(string dbPath)
        {
            // B12: Busy Timeout 5000 ms — kalau file .db sedang dikunci
            // (mis. aplikasi dibuka 2x), SQLite menunggu sampai 5 detik
            // sebelum melempar "database is locked" (default-nya 0).
            return "Data Source=" + dbPath + ";Version=3;Busy Timeout=5000;";
        }

        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(GetConnString(DbPath));
        }

        /// <summary>
        /// Menyiapkan database baru: buat 39 tabel di TokoBangunan.db
        /// (folder aplikasi, samping .exe), impor file lama satu kali
        /// kalau database masih kosong, lalu isi data contoh hanya untuk
        /// tabel yang tetap kosong. Data yang sudah ada tidak pernah
        /// ditimpa. File .db lama tidak pernah dihapus.
        /// </summary>
        public static bool EnsureDatabase(out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                Directory.CreateDirectory(BaseDir);
                using (var conn = GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(ScriptSchema, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    // Fase 2: tabel Pembelian + Penjualan lanjutan. IF NOT
                    // EXISTS, jadi database Fase 1 ikut termigrasi otomatis.
                    using (var cmd = new SQLiteCommand(ScriptSchemaFase2, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    // Fase 3: tabel Konsinyasi + Gudang. IF NOT EXISTS,
                    // jadi database Fase 1/2 ikut termigrasi otomatis.
                    using (var cmd = new SQLiteCommand(ScriptSchemaFase3, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    // Fase 4: tabel Keuangan & Akuntansi + bagan akun.
                    // IF NOT EXISTS, jadi database Fase 1/2/3 ikut
                    // termigrasi otomatis; seed akun idempotent.
                    using (var cmd = new SQLiteCommand(ScriptSchemaFase4, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd = new SQLiteCommand(ScriptSeedAkun, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Migrasi satu kali dari 3 file lama (hanya mengisi
                    // tabel yang kosong — INSERT OR IGNORE + cek COUNT).
                    MigrateOldDatabases(conn);

                    // Data contoh hanya untuk tabel yang masih kosong.
                    if (CountRows(conn, "Supplier") == 0)
                    {
                        using (var cmd = new SQLiteCommand(ScriptSeedSupplier, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    bool itemDibuatDariContoh = false;
                    if (CountRows(conn, "Item") == 0)
                    {
                        using (var cmd = new SQLiteCommand(ScriptSeedItem, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        itemDibuatDariContoh = true;
                    }
                    // Transaksi contoh hanya valid kalau item contoh yang
                    // dipakai (ID 1, 2, 4) memang baru saja dibuat — kalau
                    // item berasal dari migrasi, ID-nya bisa berarti lain.
                    if (itemDibuatDariContoh && CountRows(conn, "Transaksi") == 0)
                    {
                        using (var cmd = new SQLiteCommand(ScriptSeedTransaksi, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Fase 3: migrasi stok lama ke StokLokasi + batch awal.
                    // Dijalankan SETELAH migrasi file lama & seed, supaya
                    // semua item yang punya stok ikut termigrasi.
                    MigrasiFase3(conn);
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        private static long CountRows(SQLiteConnection conn, string tableName)
        {
            using (var cmd = new SQLiteCommand(
                "SELECT COUNT(*) FROM " + tableName, conn))
            {
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// True kalau kolom sudah ada di tabel (PRAGMA table_info).
        /// Dipakai MigrasiFase3 sebelum ALTER TABLE supaya migrasi bisa
        /// jalan berulang kali tanpa error "duplicate column name".
        /// </summary>
        private static bool KolomAda(SQLiteConnection conn, string tabel, string kolom)
        {
            using (var cmd = new SQLiteCommand("PRAGMA table_info(" + tabel + ")", conn))
            using (SQLiteDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (string.Equals(Convert.ToString(reader["name"]), kolom,
                        StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Migrasi Fase 3 (Konsinyasi + Gudang) — IDEMPOTEN, jalan setiap
        /// EnsureDatabase (semua langkah memakai guard NOT EXISTS /
        /// cek kolom, jadi jalan kedua dan seterusnya tidak mengubah apa
        /// pun). Urutan penting: dipanggil SETELAH migrasi file lama &
        /// seed, supaya stok lama (migrasi + contoh) ikut termigrasi.
        ///
        /// 1. Kolom baru di tabel lama: StokMovement.ID_Lokasi (lokasi
        ///    pergerakan stok; NULL = data lama, dianggap lokasi 1) dan
        ///    Detail_Transaksi.HPP (total HPP baris penjualan).
        /// 2. Seed Lokasi 'GU' / 'GUDANG UTAMA' + Pengaturan HPPMethod
        ///    = 'FIFO' (default).
        /// 3. Stok lama -&gt; StokLokasi (lokasi 1) untuk item Fisik/Rakitan
        ///    yang stoknya &gt; 0.
        /// 4. Batch awal 'SaldoAwal' per item yang stoknya &gt; 0 (QtyAwal =
        ///    QtySisa = stok saat itu, HargaBeli = HargaBeli item) — supaya
        ///    HPP FIFO/LIFO punya dasar untuk stok yang sudah ada.
        /// </summary>
        private static void MigrasiFase3(SQLiteConnection conn)
        {
            string sekarang = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            if (!KolomAda(conn, "StokMovement", "ID_Lokasi"))
            {
                using (var cmd = new SQLiteCommand(
                    "ALTER TABLE StokMovement ADD COLUMN ID_Lokasi INTEGER", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            if (!KolomAda(conn, "Detail_Transaksi", "HPP"))
            {
                using (var cmd = new SQLiteCommand(
                    "ALTER TABLE Detail_Transaksi ADD COLUMN HPP REAL NOT NULL DEFAULT 0", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            using (var cmd = new SQLiteCommand(
                "INSERT INTO Lokasi (Kode, Nama) SELECT 'GU', 'GUDANG UTAMA' " +
                "WHERE NOT EXISTS (SELECT 1 FROM Lokasi WHERE Kode = 'GU')", conn))
            {
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new SQLiteCommand(
                "INSERT INTO Pengaturan (Kunci, Nilai) SELECT 'HPPMethod', 'FIFO' " +
                "WHERE NOT EXISTS (SELECT 1 FROM Pengaturan WHERE Kunci = 'HPPMethod')", conn))
            {
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SQLiteCommand(
                "INSERT INTO StokLokasi (ID_Lokasi, ID_Item, Qty) " +
                "SELECT 1, ID_Item, Stok FROM Item " +
                "WHERE Stok > 0 AND Tipe IN ('Fisik','Rakitan') " +
                "AND NOT EXISTS (SELECT 1 FROM StokLokasi s " +
                "WHERE s.ID_Lokasi = 1 AND s.ID_Item = Item.ID_Item)", conn))
            {
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SQLiteCommand(
                "INSERT INTO StokBatch (ID_Item, ID_Lokasi, TanggalMasuk, " +
                "QtyAwal, QtySisa, HargaBeli, RefType, RefID) " +
                "SELECT ID_Item, 1, @Tanggal, Stok, Stok, HargaBeli, 'SaldoAwal', NULL " +
                "FROM Item WHERE Stok > 0 AND Tipe IN ('Fisik','Rakitan') " +
                "AND NOT EXISTS (SELECT 1 FROM StokBatch b " +
                "WHERE b.ID_Item = Item.ID_Item)", conn))
            {
                cmd.Parameters.AddWithValue("@Tanggal", sekarang);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Impor satu kali dari file lama di folder aplikasi (DB_Produk.db,
        /// DB_Supplier.db, DB_Transaksi.db) ke database baru. Hanya jalan
        /// untuk tabel tujuan yang masih kosong, memakai INSERT OR IGNORE
        /// dengan ID asli supaya relasi tetap utuh. File lama dibuka
        /// Read-Only dan tidak pernah diubah/dihapus. Kalau satu file lama
        /// rusak/tidak ada, file itu dilewati — database baru tetap jadi.
        /// </summary>
        private static void MigrateOldDatabases(SQLiteConnection connBaru)
        {
            MigrateOldSupplier(connBaru);
            MigrateOldProduk(connBaru);
            MigrateOldTransaksi(connBaru);
        }

        private static bool OldTableExists(SQLiteConnection connLama, string tableName)
        {
            using (var cmd = new SQLiteCommand(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@Nama",
                connLama))
            {
                cmd.Parameters.AddWithValue("@Nama", tableName);
                return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
            }
        }

        private static HashSet<string> OldColumns(SQLiteConnection connLama, string tableName)
        {
            HashSet<string> kolom = new HashSet<string>();
            using (var cmd = new SQLiteCommand("PRAGMA table_info(" + tableName + ")", connLama))
            using (SQLiteDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    kolom.Add(Convert.ToString(reader["name"]));
            }
            return kolom;
        }

        private static void MigrateOldSupplier(SQLiteConnection connBaru)
        {
            string pathLama = Path.Combine(BaseDir, "DB_Supplier.db");
            if (!File.Exists(pathLama) || CountRows(connBaru, "Supplier") > 0) return;
            try
            {
                using (var connLama = new SQLiteConnection(
                    "Data Source=" + pathLama + ";Version=3;Read Only=True;"))
                {
                    connLama.Open();
                    if (!OldTableExists(connLama, "Supplier")) return;
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Supplier, Nama_Supplier, Kontak, Alamat FROM Supplier",
                        connLama))
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            using (var ins = new SQLiteCommand(
                                "INSERT OR IGNORE INTO Supplier " +
                                "(ID_Supplier, Nama_Supplier, Kontak, Alamat) " +
                                "VALUES (@ID, @Nama, @Kontak, @Alamat)", connBaru))
                            {
                                ins.Parameters.AddWithValue("@ID", Convert.ToInt32(reader["ID_Supplier"]));
                                ins.Parameters.AddWithValue("@Nama", Convert.ToString(reader["Nama_Supplier"]));
                                ins.Parameters.AddWithValue("@Kontak", reader["Kontak"] == DBNull.Value ? (object)DBNull.Value : Convert.ToString(reader["Kontak"]));
                                ins.Parameters.AddWithValue("@Alamat", reader["Alamat"] == DBNull.Value ? (object)DBNull.Value : Convert.ToString(reader["Alamat"]));
                                ins.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch
            {
                // File lama rusak / tidak bisa dibaca: lewati saja.
                // Database baru tetap dibuat dengan data contoh.
            }
        }

        private static void MigrateOldProduk(SQLiteConnection connBaru)
        {
            string pathLama = Path.Combine(BaseDir, "DB_Produk.db");
            if (!File.Exists(pathLama) || CountRows(connBaru, "Item") > 0) return;
            try
            {
                using (var connLama = new SQLiteConnection(
                    "Data Source=" + pathLama + ";Version=3;Read Only=True;"))
                {
                    connLama.Open();
                    if (!OldTableExists(connLama, "Produk")) return;
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Produk, Nama_Produk, Harga, Stok, ID_Supplier FROM Produk",
                        connLama))
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Skema lama tidak punya HargaBeli / Kode / Tipe /
                            // Satuan: HargaBeli disamakan dengan Harga (tidak
                            // diketahui dari data lama), Kode dibuat dari ID,
                            // Tipe 'Fisik', Satuan 'pcs'.
                            int id = Convert.ToInt32(reader["ID_Produk"]);
                            decimal harga = Convert.ToDecimal(reader["Harga"]);
                            using (var ins = new SQLiteCommand(
                                "INSERT OR IGNORE INTO Item " +
                                "(ID_Item, Kode, Nama, Tipe, HargaBeli, HargaJual, " +
                                "Stok, MinStok, Satuan, ID_Supplier, Barcode) " +
                                "VALUES (@ID, @Kode, @Nama, 'Fisik', @HargaBeli, " +
                                "@HargaJual, @Stok, 0, 'pcs', @Supplier, NULL)", connBaru))
                            {
                                ins.Parameters.AddWithValue("@ID", id);
                                ins.Parameters.AddWithValue("@Kode", "BRG-" + id.ToString("D4"));
                                ins.Parameters.AddWithValue("@Nama", Convert.ToString(reader["Nama_Produk"]));
                                ins.Parameters.AddWithValue("@HargaBeli", harga);
                                ins.Parameters.AddWithValue("@HargaJual", harga);
                                ins.Parameters.AddWithValue("@Stok", Convert.ToDecimal(reader["Stok"]));
                                ins.Parameters.AddWithValue("@Supplier", Convert.ToInt32(reader["ID_Supplier"]));
                                ins.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch
            {
                // File lama rusak / tidak bisa dibaca: lewati saja.
            }
        }

        private static void MigrateOldTransaksi(SQLiteConnection connBaru)
        {
            string pathLama = Path.Combine(BaseDir, "DB_Transaksi.db");
            if (!File.Exists(pathLama) || CountRows(connBaru, "Transaksi") > 0) return;
            try
            {
                using (var connLama = new SQLiteConnection(
                    "Data Source=" + pathLama + ";Version=3;Read Only=True;"))
                {
                    connLama.Open();
                    if (!OldTableExists(connLama, "Transaksi")) return;
                    HashSet<string> kolom = OldColumns(connLama, "Transaksi");
                    bool adaBayar = kolom.Contains("Bayar");
                    bool adaKembalian = kolom.Contains("Kembalian");
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Transaksi, Tanggal, Total" +
                        (adaBayar ? ", Bayar" : "") +
                        (adaKembalian ? ", Kembalian" : "") +
                        " FROM Transaksi", connLama))
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            using (var ins = new SQLiteCommand(
                                "INSERT OR IGNORE INTO Transaksi " +
                                "(ID_Transaksi, Tanggal, Total, Bayar, Kembalian, " +
                                "ID_Pelanggan, Diskon, PoinDipakai, PoinDidapat) " +
                                "VALUES (@ID, @Tanggal, @Total, @Bayar, @Kembalian, " +
                                "NULL, 0, 0, 0)", connBaru))
                            {
                                ins.Parameters.AddWithValue("@ID", Convert.ToInt32(reader["ID_Transaksi"]));
                                ins.Parameters.AddWithValue("@Tanggal", Convert.ToString(reader["Tanggal"]));
                                ins.Parameters.AddWithValue("@Total", Convert.ToDecimal(reader["Total"]));
                                ins.Parameters.AddWithValue("@Bayar", adaBayar ? Convert.ToDecimal(reader["Bayar"]) : 0m);
                                ins.Parameters.AddWithValue("@Kembalian", adaKembalian ? Convert.ToDecimal(reader["Kembalian"]) : 0m);
                                ins.ExecuteNonQuery();
                            }
                        }
                    }

                    if (!OldTableExists(connLama, "Detail_Transaksi")) return;
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Detail, ID_Transaksi, ID_Produk, Qty, Subtotal " +
                        "FROM Detail_Transaksi", connLama))
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Skema lama tidak punya HargaSatuan: dihitung
                            // dari Subtotal / Qty (0 kalau Qty 0).
                            decimal qty = Convert.ToDecimal(reader["Qty"]);
                            decimal subtotal = Convert.ToDecimal(reader["Subtotal"]);
                            decimal hargaSatuan = qty != 0 ? subtotal / qty : 0m;
                            using (var ins = new SQLiteCommand(
                                "INSERT OR IGNORE INTO Detail_Transaksi " +
                                "(ID_Detail, ID_Transaksi, ID_Item, Qty, HargaSatuan, " +
                                "DiskonItem, Subtotal) " +
                                "VALUES (@ID, @IDTrans, @IDItem, @Qty, @Harga, 0, @Subtotal)",
                                connBaru))
                            {
                                ins.Parameters.AddWithValue("@ID", Convert.ToInt32(reader["ID_Detail"]));
                                ins.Parameters.AddWithValue("@IDTrans", Convert.ToInt32(reader["ID_Transaksi"]));
                                ins.Parameters.AddWithValue("@IDItem", Convert.ToInt32(reader["ID_Produk"]));
                                ins.Parameters.AddWithValue("@Qty", qty);
                                ins.Parameters.AddWithValue("@Harga", hargaSatuan);
                                ins.Parameters.AddWithValue("@Subtotal", subtotal);
                                ins.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch
            {
                // File lama rusak / tidak bisa dibaca: lewati saja.
            }
        }

        public static bool TestConnection(out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT 1", conn))
                    {
                        cmd.ExecuteScalar();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}
