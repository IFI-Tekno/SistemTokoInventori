# SistemTokoInventori — Fase 2 (v2) — Catatan Rebuild

Tanggal: 8 Nov 2026. Lokasi kerja: `~/workspace/sistem-toko-inventori-v2`
(dari salinan versi Guna). TIDAK ADA yang diubah di
`~/workspace/sistem-toko-inventori-review` maupun
`~/workspace/sistem-toko-inventori-guna`. Tidak ada yang di-push ke GitHub.

> **JUJUR: tidak ada compile test.** Tidak ada toolchain .NET/Mono di VM
> Linux ini, jadi kode TIDAK PERNAH di-build. Pengguna WAJIB Build
> (Ctrl+Shift+B) di Visual Studio sebelum menjalankan. Semua verifikasi di
> bawah ini adalah pengujian Python terhadap SQL/tabel yang diekstrak dari
> file C# + pemeriksaan konsistensi Designer/code-behind — bukan kompilasi.

## 1. Arsitektur (ASCII)

```
                    +------------------ FormMain (Form) ------------------+
                    | Sidebar (Guna2, Theme.cs)                           |
                    |  Dashboard | Kasir | Pembelian | Penjualan | Master |
                    |  Data | Laporan | Keluar                            |
                    +----------+------------------------------------------+
                               |
              +----------------+-----------------+----------------+
              | pnlMain (Dashboard, tetap di FormMain)                |
              |  KPI Omzet / Produk / Transaksi / Stok Menipis        |
              |  Produk Terlaris (Top 5) | Stok Menipis              |
              +-------------------------------------------------------+
              | pnlContent (Panel, Dock=Fill, 1 UserControl aktif)    |
              |  KasirControl | PembelianControl | PenjualanControl   |
              |  MasterDataControl | LaporanControl                  |
              |  (dibuat sekali/lazy, dipakai ulang; keranjang Kasir  |
              |   tetap ada saat pindah tab lalu kembali)             |
              +-------------------------------------------------------+
                               |
                    Dialog modal (tetap Form):
                    FormBayar (Tunai/Tempo) + FormStruk (struk tampil)
                    FormTukarTambah + FormCatatPembayaran +
                    FormDetailPembayaran + FormHistoryHarga
                               |
                    +----------v-----------+
                    | ConnectionManager    |  SATU koneksi: GetConnection()
                    | TokoBangunan.db      |  Busy Timeout=5000 (B12)
                    +----------+-----------+
                               |
              %LocalAppData%\SistemTokoInventori\TokoBangunan.db
              (file .db lama di folder .exe hanya dibaca saat migrasi,
               TIDAK PERNAH dihapus/diubah)
```

Sidebar Fase 2: **Dashboard, Kasir, Pembelian, Penjualan, Master Data,
Laporan** — tidak ada tombol untuk modul yang belum ada (tidak ada fake UI).
Tombol "Lihat di Inventori" di Dashboard menjadi "Lihat di Master Data".

## 2. Skema — satu database `TokoBangunan.db` (8 tabel)

```sql
Supplier        (ID_Supplier PK, Nama_Supplier, Kontak, Alamat)
Item            (ID_Item PK, Kode UNIQUE, Nama, Tipe CHECK IN
                 ('Fisik','Rakitan','NonInventori','Jasa'),
                 HargaBeli REAL, HargaJual REAL, Stok REAL,
                 MinStok REAL DEFAULT 0, Satuan, ID_Supplier FK,
                 Barcode)
Pelanggan       (ID_Pelanggan PK, Nama, Telepon, Alamat, Poin INT DEFAULT 0)
DiskonPeriode   (ID_Diskon PK, Nama, Persen REAL, TglMulai TEXT,
                 TglSelesai TEXT, Aktif INT DEFAULT 1)
Transaksi       (ID_Transaksi PK, Tanggal TEXT, Total REAL, Bayar REAL,
                 Kembalian REAL, ID_Pelanggan NULL FK, Diskon REAL DEFAULT 0,
                 PoinDipakai INT DEFAULT 0, PoinDidapat INT DEFAULT 0)
Detail_Transaksi(ID_Detail PK, ID_Transaksi FK, ID_Item FK, Qty REAL,
                 HargaSatuan REAL, DiskonItem REAL DEFAULT 0, Subtotal REAL)
HargaJualHistory(ID_History PK, ID_Item FK, HargaLama REAL, HargaBaru REAL,
                 Tanggal TEXT, Keterangan TEXT)
StokMovement    (ID_Movement PK, ID_Item FK, Tanggal TEXT, Jenis TEXT,
                 Qty REAL, StokSesudah REAL, RefID TEXT, Keterangan TEXT)
```

Tanggal disimpan sebagai teks `yyyy-MM-dd HH:mm:ss` (seperti versi lama);
tanggal diskon sebagai `yyyy-MM-dd`. Stok & Qty REAL (boleh desimal,
mis. 1,5 kg) — Qty di Kasir tetap bilangan bulat (NumericUpDown).

### Migrasi satu kali dari 3 file lama

`ConnectionManager.EnsureDatabase()` (pengganti `EnsureDatabases`):
buat folder `%LocalAppData%\SistemTokoInventori`, buat 8 tabel, lalu
`MigrateOldDatabases()` — hanya mengisi tabel tujuan yang masih kosong
(`COUNT(*) == 0` + `INSERT OR IGNORE`), file lama dibuka **Read-Only**:

- `DB_Produk.db` → `Item`: ID dipertahankan; `Kode = "BRG-" + ID (4 digit)`;
  `Tipe = 'Fisik'`, `Satuan = 'pcs'`, `MinStok = 0`, `Barcode = NULL`;
  **`HargaBeli` disamakan dengan `Harga`** (skema lama tidak punya harga
  beli — tidak diketahui, jadi margin awal 0; ubah manual di Master Data).
- `DB_Supplier.db` → `Supplier`: ID + semua kolom dipertahankan.
- `DB_Transaksi.db` → `Transaksi` + `Detail_Transaksi`: ID dipertahankan;
  `Bayar`/`Kembalian` ikut kalau kolomnya ada (DB sangat lama tanpa kolom
  itu → 0); `HargaSatuan = Subtotal / Qty` (0 kalau Qty 0).
- File lama rusak/tidak ada → dilewati diam-diam; database baru tetap jadi.
- **File lama tidak pernah dihapus atau ditulis.**

Data contoh (supplier + 4 item + 2 transaksi) hanya diisi untuk tabel yang
tetap kosong setelah migrasi; transaksi contoh hanya kalau item contoh yang
dipakai (ID 1, 2, 4) memang baru dibuat dari seed.

API lama HILANG: `GetProdukConnection` / `GetTransaksiConnection` /
`GetSupplierConnection` / `EnsureDatabases` / `TestAllConnections` diganti
`GetConnection()` / `EnsureDatabase()` / `TestConnection()`.

## 3. Yang berubah (file)

### Dihapus

- `FormKasir.cs` + `.Designer.cs` + `.resx`
- `FormInventori.cs` + `.Designer.cs` + `.resx`
- `FormLaporan.cs` + `.Designer.cs` + `.resx`

`FormInventori` tidak diganti 1:1 — CRUD produk/supplier-nya dilebur ke
`MasterDataControl`; tidak ada lagi modul "Inventori" terpisah di Fase 2.
Stok terlihat di Dashboard + tab Item.

### Dibuat

- `KasirControl.cs` + `KasirControl.Designer.cs` — konversi FormKasir.
- `LaporanControl.cs` + `LaporanControl.Designer.cs` — konversi FormLaporan.
- `MasterDataControl.cs` — BARU, UserControl tanpa Designer (UI dibangun
  dari kode di satu file supaya semua kontrol + wiring bisa dibaca).
- `Barcode39.cs` — Code 39 tanpa library (tabel encoding + render
  `System.Drawing` + validasi).

### Diubah

- `ConnectionManager.cs` — ditulis ulang total (lihat §2).
- `FormMain.cs` + `FormMain.Designer.cs` — shell single-window: sidebar
  (tombol `btnNavInventori` → `btnNavMasterData`, `btnLihatInventori` →
  `btnLihatMasterData`), panel konten baru `pnlContent` (Dock=Fill,
  awalnya `Visible=false`), `TampilkanKonten()` + `ShowDashboard()`,
  kontrol dibuat sekali (lazy) + `RefreshData()` tiap kali dibuka.
  Dashboard: query satu DB (JOIN asli untuk Terlaris; stok menipis hanya
  `Tipe IN ('Fisik','Rakitan')`).
- `FormStruk.cs` — hanya nama kolom keranjang: `Nama_Produk`/`Harga` →
  `Nama`/`HargaJual` (kolom keranjang v2).
- `FormBayar.cs` — hanya komentar (`FormKasir` → `KasirControl`,
  `FormInventori.TryParseDecimal` → `MasterDataControl.TryParseDecimal`).
- `SistemTokoInventori.csproj` — hapus 3 form lama (+ resx), tambah
  `Barcode39.cs`, `KasirControl`, `LaporanControl`, `MasterDataControl`
  (SubType UserControl). Referensi `libs\Guna.UI2.dll` dan
  `libs\System.Data.SQLite.dll` tidak berubah.

### Tidak diubah

`Program.cs`, `Theme.cs` (palet tetap sumber kebenaran), `FormBayar`,
`FormStruk` (selain kolom di atas), `libs/` (DLL tetap).

## 4. Cara kerja tiap bagian

### Shell (FormMain)

Klik nav → kontrol dibuat sekali kalau belum ada → `RefreshData()` →
`TampilkanKonten(kontrol)` (Dock=Fill di `pnlContent`, kontrol lain
disembunyikan, `pnlMain.Visible=false`). Dashboard → `pnlContent`
disembunyikan, `pnlMain` tampil, `LoadDashboard()` jalan lagi.
`SetActiveNav` mewarnai tombol aktif (Theme.cs).

### Kasir (KasirControl)

Sama seperti FormKasir Guna, dengan adaptasi skema: `Produk`→`Item`,
`ID_Produk`→`ID_Item`, `Harga`→`HargaJual`. Keranjang (kolom
`ID_Item, Nama, HargaJual, Qty, Subtotal, Tipe` — kolom `Tipe`
disembunyikan di grid) dipertahankan antar-tab karena kontrol dipakai ulang.

- Grid: `Stok > 0 OR Tipe IN ('Jasa','NonInventori')` — Jasa/NonInventori
  tetap tampil walau Stok 0 (tidak dilacak).
- `btnTambahCart`: guard B3 (qty ≤ 0 ditolak), cek stok hanya untuk tipe
  yang melacak stok.
- `btnProses`: `CekStokCukup` (lewati Jasa/NonInventori) → `FormBayar`
  (blokir Bayar < Total) → **langkah 1**: `UPDATE Item SET Stok=Stok-@Qty
  WHERE ID_Item=@ID AND Stok>=@Qty` per item stok (satu transaksi;
  gagal → rollback, pesan, batal) + baca `StokSesudah` → flag
  `stokDikurangi=true` (B1) → **langkah 2**: `INSERT Transaksi`
  (Bayar/Kembalian + `ID_Pelanggan NULL, Diskon 0, PoinDipakai 0,
  PoinDidapat 0`) + `INSERT Detail_Transaksi` (HargaSatuan, DiskonItem 0)
  + `INSERT StokMovement` per item stok (`Jenis='Jual'`, `Qty=-qty`,
  `RefID=ID_Transaksi`). Gagal langkah 2 → `KembalikanStok` (satu
  transaksi, hanya item stok) — persis logika B1 lama.
- Langkah 1 dan 2 sengaja tetap dua transaksi terpisah walau sekarang
  satu database, supaya logika kompensasi B1 tidak berubah.
- `FormStruk` tampil setelah transaksi tersimpan.

### Laporan (LaporanControl)

Satu query JOIN (tidak ada lagi gabung manual 3 DB di C#). Filter tanggal
di `ON` via `EXISTS` — sengaja BUKAN di `WHERE`: kalau di `WHERE`, item
yang penjualannya hanya di luar rentang akan hilang dari laporan; di `ON`,
detail di luar rentang tidak ikut JOIN dan item tetap tampil (0).
Semua item tampil (LEFT JOIN), terlaris di atas, `Total Omzet` = jumlah
semua baris tampil. Tombol "Load Laporan" = reset filter + pencarian.
Pencarian memfilter `Nama` (RowFilter, escape `'`/`%`/`_`/`[`).

### Master Data (MasterDataControl) — 4 tab

- **Item**: grid (Kode, Nama, Tipe, HargaJual, Stok, Satuan, Supplier)
  + cari (Kode/Nama) + form CRUD (Kode, Nama, Tipe dropdown,
  HargaBeli/HargaJual/Stok/MinStok/Satuan, supplier dropdown, Barcode).
  Kode kosong → disarankan dari Nama (huruf/angka saja, huruf besar,
  maks 8 karakter, mis. "Indomie Goreng" → "INDOMIEG"; kalau dipakai,
  "-2", "-3", …). Kode duplikat diblokir (cek + UNIQUE). Update:
  HargaJual berubah → baris `HargaJualHistory` (lama, baru, now,
  'Ubah harga'); Stok berubah (tipe stok) → baris `StokMovement`
  (`Jenis='Penyesuaian'`, `Qty=selisih`). Tambah item dengan stok > 0 →
  `StokMovement 'Masuk'` (stok awal). Hapus: B4 — item dengan baris
  `Detail_Transaksi` diblokir; kalau lolos, `HargaJualHistory` +
  `StokMovement` milik item itu ikut terhapus dalam transaksi yang sama
  (log itu tidak bermakna tanpa itemnya).
- **Supplier**: CRUD pindahan FormInventori. Hapus diblokir kalau masih
  dipakai item.
- **Pelanggan**: CRUD (Nama, Telepon, Alamat, Poin). Poin di sini hanya
  saldo awal (kosong = 0); ada label penjelasan bahwa perolehan &
  pemakaian poin di Kasir adalah Fase 2. Hapus diblokir kalau sudah
  punya transaksi.
- **Diskon Periode**: CRUD (Nama, Persen 0–100, TglMulai, TglSelesai,
  Aktif checkbox). Ada label: penerapan otomatis di Kasir adalah Fase 2
  — saat ini hanya master data.
- **Cetak Barcode** (tab Item): nilai = `Barcode` kalau terisi, kalau
  tidak pakai `Kode`. `Barcode39.Validate` menolak kosong, `*`, dan
  karakter di luar `A-Z 0-9 spasi - . $ / + %` dengan pesan jelas.
  Render Code 39 (narrow 2px, tinggi 80px, quiet zone 10x, rasio
  wide:narrow 1:3, gap antar-karakter 1 narrow) → `PrintPreviewDialog`
  → `PrintDocument` (nama item + harga + gambar barcode + teks
  `*NILAI*`).

### Barcode39 — tabel encoding

Tabel ditulis sendiri (43 karakter + `*` start/stop), diverifikasi
terhadap dua sumber independen (wiki.tcl-lang.org — kodenya dicatat
"tested with a real scanner" — dan technoriversoft.com) plus cek
struktural: tiap pola 9 elemen, tepat 3 wide; 40 karakter asli = 2 wide
bar + 1 wide space; tiap grup (digit, A–J, K–T, U–Z) berbagi satu posisi
wide-space. Catatan: transkripsi tangan pertama sempat salah untuk
A, B, D, E, F, G, I, C, H, J — ketahuan dan diperbaiki lewat verifikasi
mekanis itu (lihat §5).

## 5. Verifikasi

### `test_v2_phase1.py` — 61/61 PASS (exit 0)

`python3 test_v2_phase1.py` di `~/workspace/sistem-toko-inventori-v2`.
SQL/skema/tabel Code 39 diambil langsung dari file C#, dijalankan di
SQLite asli; alur bisnis dimodelkan dengan statement yang sama seperti
di `KasirControl.cs`/`MasterDataControl.cs`.

- S1 skema: 8 tabel + kolom kunci + CHECK Tipe + Busy Timeout=5000 +
  path `%LocalAppData%\SistemTokoInventori\TokoBangunan.db`.
- S2 seed: 3 supplier, 4 item, 2 transaksi, 3 detail; query Laporan
  tanpa filter → omzet Rp 53.000, 4 item (Susu 0); filter 2026-10-20 →
  hanya Indomie Rp 21.000, omzet Rp 21.000, item lain tetap tampil (0).
- S3 migrasi: dari 3 file .db lama contoh → 2 supplier + 3 item +
  1 transaksi + 1 detail; ID/nama/harga/stok/supplier dipertahankan
  (termasuk ID 7 tidak berurutan); `HargaSatuan = Subtotal/Qty`;
  jalan 2x tidak menduplikasi; file lama tidak berubah (hash SHA-256
  sebelum/sesudah sama) dan tidak dihapus; C# hanya impor saat tabel
  kosong + tanpa `File.Delete`.
- S4 riwayat harga: update 3500→4000 → 1 baris HargaJualHistory.
- S5 Code 39: 44 pola ter-parse, semua == tabel standar, tiap pola
  9 elemen + tepat 3 wide + unik; lebar "A123" narrow=2 → 230px
  (dihitung tangan); Validate menolak `*`, `!`, kosong.
- S6 Jasa/NonInventori: cek kode (`MelacakStok` di CekStokCukup,
  langkah 1, KembalikanStok) + perilaku: Fisik 10→8, Rakitan 5→4,
  Jasa/NonInventori tetap 0.
- S7 B1 di skema baru: langkah-1 gagal → stok tetap 100; langkah-1
  commit + langkah-2 gagal → restore → 100; sukses → stok 94,
  1 transaksi + 1 detail + 1 movement `Jual -6`.
- S8 B4 + Kode unik: item ber-riwayat COUNT>0 (blokir), tanpa riwayat
  COUNT=0; UNIQUE menolak Kode kedua.
- S9 konsistensi: semua kontrol yang dipakai code-behind ada di Designer
  + semua event handler ter-wire ada (Kasir 4, Laporan 3, FormMain 6,
  MasterData 22 handler); 4 tab Master Data; sidebar tanpa "Inventori";
  shell (`pnlContent` + `TampilkanKonten`, kontrol dibuat sekali);
  API 3-DB lama hilang; tanpa `UseVisualStyleBackColor` /
  `BeginInit` pada `numQty` / `txt*.Clear()`; FormStruk pakai kolom v2;
  Laporan satu query JOIN.

Dua bug nyata ketahuan & diperbaiki berkat tes ini (bukan ditebak):
1. **Tabel Code 39** — 7 pola salah transkripsi tangan (A, B, D, E, F,
   G, I; plus C, H, J di putaran pertama). Diperbaiki setelah verifikasi
   mekanis terhadap tabel Tcl + cek struktural (2 wide bar + 1 wide
   space, grup wide-space). Tes S5 sekarang membandingkan ke-44 pola
   dengan tabel standar yang sama-sama diverifikasi mekanis.
2. **Filter tanggal Laporan** — versi pertama menaruh filter di `WHERE`,
   sehingga item yang penjualannya hanya di luar rentang HILANG dari
   laporan (tes S2 gagal: 2 baris, bukan 4). Dipindah ke `ON` via
   `EXISTS`; tes S2 sekarang lulus (4 baris, item di luar rentang = 0).

### Suite Python lama — TIDAK berlaku untuk v2

`test_b1_b12.py`, `test_p1.py`, `test_guna_features.py`,
`verify_restyle.py` membaca file `FormKasir.cs` / `FormInventori.cs`
yang sudah dihapus dan crash `FileNotFoundError` — sesuai desain, bukan
kegagalan v2. Logika yang mereka uji (B1, B2, B3, B4, Guna) sudah
dicakup ulang oleh `test_v2_phase1.py` pada skema baru (S2–S9).
Tidak ada suite lama yang bisa di-run ulang secara bermakna.

### Re-read manual

Semua file yang disentuh dibaca ulang end-to-end setelah diedit:
`ConnectionManager.cs`, `Barcode39.cs`, `KasirControl.cs` +
Designer, `LaporanControl.cs` + Designer, `MasterDataControl.cs`
(1659 baris), `FormMain.cs` + Designer, `FormStruk.cs`, `.csproj`.
Satu dead code dibuang saat re-read (`tipeById` di KasirControl yang
tidak terpakai — Tipe sudah dibawa per baris keranjang).

### Yang TIDAK terverifikasi

- **Kompilasi / runtime**: tidak ada .NET di VM. Kemungkinan masalah
  yang hanya ketahuan saat Build di Visual Studio: (a) properti Guna2
  yang dipakai MasterDataControl dari kode (`TabMenuBackColor` dkk.
  lewat `Theme.StyleTabControl` — sudah dipakai FormInventori lama, jadi
  risikonya rendah; `Guna2CheckBox` dihindari — dipakai `CheckBox`
  standar); (b) layout tab Master Data (koordinat absolut — perlu cek
  visual saat dibuka, terutama lebar tab di layar kecil); (c) `PrintPreviewDialog`
  butuh printer terinstal untuk print beneran (preview tetap jalan).
- Barcode Code 39 belum dipindai dengan scanner fisik — baru
  diverifikasi terhadap tabel standar + cek struktural.

## 6. Fase 1 vs fase berikut (tidak dibangun di sini)

**Fase 1 (selesai di sini):** shell single-window; satu database +
migrasi; Master Data (Item/Supplier/Pelanggan/Diskon Periode);
Kasir + Laporan di skema baru; StokMovement ditulis Kasir + editor Item;
HargaJualHistory; Cetak Barcode Code 39.

**Bukan Fase 1 (sengaja tidak dimulai):** Konsinyasi, Gudang, Keuangan,
Mobile, Assembly/Rakitan (stok Rakitan saat ini seperti Fisik di POS),
kartu stok & FIFO (membaca `StokMovement` — log sudah ditulis),
login/roles, backup/export. Pembelian + Penjualan lanjutan sudah selesai
di Fase 2 (lihat bagian 8).

**Keputusan yang diambil di luar spek tertulis (dicatat di sini):**
- Migrasi juga mengimpor `DB_Transaksi.db` (speks hanya menyebut Produk
  + Supplier) supaya riwayat penjualan & aturan B4 tidak hilang.
- `HargaBeli` hasil migrasi = `HargaJual` lama (harga beli tidak ada di
  data lama).
- Hapus item menghapus `HargaJualHistory` + `StokMovement` miliknya
  (dalam transaksi yang sama); riwayat penjualan tetap memblokir (B4).
- Hapus pelanggan diblokir kalau sudah punya transaksi (melindungi
  riwayat — analog B4).
- `chkAktif` Diskon memakai `CheckBox` standar, bukan `Guna2CheckBox`
  (menghindari properti Guna2 yang belum terverifikasi di DLL).
- MasterDataControl dibangun dari kode (tanpa Designer) — satu file
  yang bisa dibaca penuh; `Theme.StyleTabControl`/`StyleGrid`/dll.
  tetap dipakai.

## 7. Cara menjalankan (untuk pengguna)

1. Buka `SistemTokoInventori.sln` di Visual Studio → **Build**
   (Ctrl+Shift+B). Kalau ada error, kirim pesan errornya.
2. Jalankan. Database baru dibuat otomatis di
   `%LocalAppData%\SistemTokoInventori\TokoBangunan.db`. Kalau file
   `DB_Produk.db` / `DB_Supplier.db` / `DB_Transaksi.db` lama ada di
   folder aplikasi, isinya diimpor sekali — file lama tetap ada.
3. Sidebar: Dashboard → Kasir → Pembelian → Penjualan → Master Data → Laporan.

## 8. Fase 2 — Pembelian + Penjualan lanjutan (selesai 8 Nov 2026)

Gaya: modern flat solid, **tanpa gradasi di mana pun** — `Theme.BuildHeader`
diubah dari `Guna2GradientPanel` (Primary → PrimaryDark) menjadi
`Guna2Panel` solid Primary. Tidak ada lagi `Guna2GradientPanel` /
`GradientMode` di proyek (dicek test). Palet Theme.cs tidak berubah.

### 8.1 Skema baru (9 tabel, `ScriptSchemaFase2` di ConnectionManager.cs)

`CREATE TABLE IF NOT EXISTS` — database yang dibuat di Fase 1 cukup
menjalankan `EnsureDatabase` lagi; tabel Fase 2 dibuat otomatis, data lama
tidak disentuh. Total sekarang 17 tabel.

```sql
PurchaseOrder (ID_PO PK, NoPO TEXT UNIQUE, ID_Supplier FK, Tanggal TEXT,
               Status CHECK IN ('Draft','Dikirim','Diterima','Dibatalkan'),
               Total REAL, DP REAL DEFAULT 0, Catatan TEXT)
PODetail      (ID_PODetail PK, ID_PO FK, ID_Item FK, Qty REAL,
               HargaBeli REAL, Subtotal REAL)
Hutang        (ID_Hutang PK, RefType TEXT, RefID INTEGER, ID_Supplier FK,
               Tanggal TEXT, JatuhTempo TEXT, Total REAL, Sisa REAL,
               Status CHECK IN ('Belum Lunas','Lunas'))
HutangBayar   (ID_HutangBayar PK, ID_Hutang FK, Tanggal TEXT,
               Jumlah REAL, Keterangan TEXT)
SalesOrder    (ID_SO PK, NoSO TEXT UNIQUE, ID_Pelanggan FK, Tanggal TEXT,
               Status CHECK IN ('Draft','Diproses','Selesai','Dibatalkan'),
               Total REAL, Catatan TEXT)
SODetail      (ID_SODetail PK, ID_SO FK, ID_Item FK, Qty REAL,
               HargaJual REAL, DiskonItem REAL DEFAULT 0, Subtotal REAL)
Piutang       (ID_Piutang PK, ID_Transaksi INTEGER NULL FK, NoFaktur TEXT,
               ID_Pelanggan FK, Tanggal TEXT, JatuhTempo TEXT, Total REAL,
               Sisa REAL, Status CHECK IN ('Belum Lunas','Lunas'))
PiutangBayar  (ID_PiutangBayar PK, ID_Piutang FK, Tanggal TEXT,
               Jumlah REAL, Keterangan TEXT)
TukarTambah   (ID_TukarTambah PK, ID_Transaksi FK, DeskripsiBarang TEXT,
               ID_ItemMasuk INTEGER NULL FK, QtyMasuk REAL DEFAULT 0,
               NilaiTukar REAL, Keterangan TEXT)
```

Penomoran: `NoPO = 'PO-' + yyyy + '-' + urutan 4 digit` (dihitung dari
`COUNT(*) WHERE NoPO LIKE 'PO-yyyy-%'` + 1 — PO tidak pernah dihapus, jadi
nomor tidak dipakai ulang); `NoSO` sama dengan prefix `SO-`; `NoFaktur`
piutang = `'INV-' + ID_Transaksi` (nomor yang sama dengan yang tampil di
struk).

### 8.2 Pembelian (`PembelianControl.cs`, code-built, 2 tab)

- **Pesanan Pembelian:** grid (NoPO, Supplier, Tanggal, Total, Status) +
  cari (NoPO / supplier) + form (supplier, item + Qty + HargaBeli — harga
  terisi otomatis dari HargaBeli item, total auto, DP, Catatan).
  Status: Draft → Dikirim (tombol "Kirim", guard `WHERE Status='Draft'`) →
  Diterima (tombol "Terima Barang", guard `WHERE Status='Dikirim'`).
  **Terima Barang = SATU transaksi:** kunci status Diterima; per item
  PODetail yang melacak stok (Fisik/Rakitan — Jasa/NonInventori dilewati):
  `Item.Stok += Qty` + `StokMovement 'Masuk'` (RefID `PO-<NoPO>`,
  Keterangan 'Terima PO'); kalau `Total > DP` buat satu baris Hutang
  (RefType='PO', JatuhTempo = tanggal terima + 30 hari, Sisa = Total - DP,
  Status 'Belum Lunas'); kalau `DP >= Total` tidak ada Hutang.
  Terima kedua diblokir (guard rowcount 0). Draft bisa diubah / dibatalkan
  ("Batalkan", guard Draft); **Diterima / Dibatalkan tidak bisa diubah atau
  dibatalkan** — form dikunci (`bolehEditPO`) dan tombol dimatikan.
  "Cetak PO": `PrintPreviewDialog` + `PrintDocument` (header, supplier,
  tabel item, total, DP, sisa hutang).
- **Hutang:** grid (Supplier, Tanggal, JatuhTempo, Total, Sisa, Status) +
  cari + "Catat Pembayaran" (modal `FormCatatPembayaran`: jumlah, tanggal,
  keterangan; validasi jumlah > 0 dan <= Sisa, dicek lagi di SQL dengan
  `WHERE Sisa >= @Jumlah` + baca ulang Sisa di dalam transaksi; Sisa
  berkurang, Status jadi 'Lunas' kalau Sisa 0 via
  `CASE WHEN Sisa - @Jumlah <= 0`) + "Detail" (modal
  `FormDetailPembayaran`: riwayat `HutangBayar` + ringkasan
  total/dibayar/sisa). Baris jatuh tempo **merah** kalau sudah lewat dan
  belum lunas (`CellFormatting`).

### 8.3 Penjualan (`PenjualanControl.cs`, code-built, 2 tab)

- **Pesanan Penjualan:** seperti PO tapi pelanggan + item picker dengan
  HargaJual + DiskonItem per item (harus < HargaJual); Subtotal =
  (HargaJual − DiskonItem) × Qty. Draft bisa diubah / dibatalkan.
  **"Muat ke Kasir":** hanya kalau keranjang Kasir kosong (kalau tidak,
  ditolak dengan pesan) dan SO masih Draft / Diproses. Isi SO dimuat ke
  keranjang (`KasirControl.MuatSalesOrder`: HargaJual & DiskonItem per item
  disimpan di kolom baru keranjang `DiskonItem`, pelanggan SO dipilih di
  Kasir), status SO jadi 'Diproses', shell pindah ke tab Kasir
  (`FormMain.Kasir` + `TampilkanKasir()`; `Shell` diset FormMain saat
  kontrol pertama dibuat). Kalau transaksi Kasir selesai, SO jadi 'Selesai'
  (di langkah 2 Kasir, dalam transaksi yang sama). Kalau transaksi
  dibatalkan (FormBayar Cancel / stok gagal), SO tetap 'Diproses' dan bisa
  dimuat ulang setelah keranjang dikosongkan (keranjang kosong mereset
  `idSalesOrderAktif` di Kasir).
- **Piutang:** grid (NoFaktur, Pelanggan, Tanggal, JatuhTempo, Total, Sisa,
  Status) + cari + "Catat Pembayaran" + "Detail" — sama persis dengan
  Hutang (modal yang sama dipakai ulang). Piutang dibuat otomatis dari
  pembayaran Tempo di Kasir (lihat 8.4).

### 8.4 Kasir — pelanggan, diskon, poin, tukar tambah, Tempo

Baris baru di atas grid produk: dropdown pelanggan (baris pertama
"Umum (tanpa pelanggan)"), label saldo poin, input "Poin dipakai", label
hijau diskon aktif. Baris ringkasan di atas panel total menampilkan
Subtotal • Diskon • Poin dipakai • Tukar tambah • Poin didapat.

- **Diskon periode (otomatis):** hanya kalau pelanggan dipilih (Umum =
  tanpa diskon). Query: `Aktif=1 AND TglMulai <= hari ini <= TglSelesai`,
  `ORDER BY Persen DESC LIMIT 1` (persen tertinggi). Berlaku untuk
  subtotal keranjang (semua item, sudah termasuk DiskonItem SO).
  Label hijau: `Diskon <Nama> <Persen>% aktif`.
- **Poin:** dapat = `floor((subtotal − diskon) / 10000)`, hanya kalau ada
  pelanggan. Poin dipakai & tukar tambah adalah **cara bayar, bukan
  potongan harga** — tidak mengurangi dasar perolehan poin. 1 poin =
  Rp 100. Maks pakai = `min(saldo pelanggan, floor((subtotal−diskon)/100))`
  (spinner dibatasi saldo; batas total dijaga di `HitungTotal` dengan
  clamp). Saldo diubah di langkah 2: `Poin = Poin − Dipakai + Didapat`.
- **Tukar tambah:** tombol "Tukar Tambah" → modal `FormTukarTambah`
  (deskripsi wajib; item masuk opsional — hanya Fisik/Rakitan, karena
  stoknya yang naik; qty masuk; nilai tukar). Nilai tidak boleh melebihi
  total setelah diskon & poin (dicek di modal via `MaxNilai` dan dijaga
  lagi di `HitungTotal`). Saat transaksi selesai (langkah 2): baris
  `TukarTambah` disimpan; kalau ada item masuk, stoknya naik +
  `StokMovement 'Masuk'` (Keterangan 'Tukar tambah'). Modal bisa
  menghapus tukar tambah yang sudah ada (tombol "Hapus").
- **Pembayaran Tempo (`FormBayar`):** radio Tunai / Tempo + picker
  Jatuh Tempo (default hari ini + 14 hari, hanya tampil untuk Tempo).
  Tempo **butuh pelanggan** (dibloki dengan pesan kalau Umum). Bayar
  boleh 0 (full tempo) sampai Total (DP sebagian); tidak ada kembalian —
  yang tampil "Sisa piutang". Tunai tidak berubah (Bayar >= Total).
  Saat transaksi selesai (langkah 2): kalau Tempo dan `Total − Bayar > 0`,
  buat baris `Piutang` (NoFaktur `INV-<id transaksi>`, JatuhTempo dari
  picker, Sisa = Total − Bayar, Status 'Belum Lunas'). Kalau lunas di
  tempat (sisa 0), tidak ada piutang.
- **Struk (`FormStruk`):** konstruktor baru dengan rincian — Subtotal,
  Diskon, Poin dipakai, Tukar tambah, Total, Bayar, lalu Kembalian
  (Tunai) atau Sisa piutang + Jatuh tempo (Tempo). Konstruktor lama
  didelegasikan (rincian 0, Tunai). Baris item memakai harga efektif
  (HargaJual − DiskonItem) supaya `qty × harga = subtotal` tetap
  konsisten untuk keranjang dari SO.
- **Yang dipertahankan:** alur dua langkah B1 tidak berubah (langkah 1
  kurangi stok dengan guard `Stok >= Qty` + flag `stokDikurangi`;
  langkah 2 kini menyimpan `ID_Pelanggan`, `Diskon`, `PoinDipakai`,
  `PoinDidapat` + `Detail_Transaksi.DiskonItem` (dari kolom keranjang
  baru) + poin + piutang + tukar tambah + status SO, **semuanya dalam
  satu transaksi langkah 2** — gagal di mana pun = rollback semua, lalu
  `KembalikanStok` seperti sebelumnya). `CekStokCukup` tetap sebelum
  FormBayar. Cari produk, filter tanggal Laporan, P0/P1 tidak berubah.

### 8.5 History Harga (`FormHistoryHarga.cs`)

Tombol "History Harga" di tab Item Master Data (di samping "Cetak
Barcode") → modal read-only berisi `HargaJualHistory` item yang dipilih
(Tanggal, HargaLama → HargaBaru, Keterangan), terbaru dulu. Tabelnya
sudah dicatat otomatis sejak Fase 1. Catatan di tab Pelanggan & Diskon
diperbarui ke aturan Fase 2 (rumus poin + persen tertinggi).

### 8.6 File baru / diubah

- Baru: `PembelianControl.cs`, `PenjualanControl.cs`,
  `FormTukarTambah.cs`, `FormCatatPembayaran.cs`,
  `FormDetailPembayaran.cs`, `FormHistoryHarga.cs` (semua code-built,
  tanpa Designer — mengikuti pola MasterDataControl; terdaftar di
  `.csproj`).
- Diubah: `ConnectionManager.cs` (9 tabel + `EnsureDatabase`),
  `Theme.cs` (`BuildHeader` solid), `FormMain.cs` + `FormMain.Designer.cs`
  (2 tombol sidebar, `Kasir` + `TampilkanKasir`), `KasirControl.cs` +
  `KasirControl.Designer.cs` (pelanggan/diskon/poin/tukar/SO),
  `FormBayar.cs` + `FormBayar.Designer.cs` (Tempo),
  `FormStruk.cs` (rincian + Tempo), `MasterDataControl.cs` (tombol
  History Harga + catatan poin/diskon).

### 8.7 Verifikasi (8 Nov 2026)

- `test_v2_phase2.py` (baru): **111/111 PASS, 0 FAIL** — skema (9 tabel,
  kolom, CHECK, UNIQUE), PO lifecycle (Draft→Dikirim→Diterima, stok
  Fisik 100→110 / Rakitan 20→21 / Jasa tetap 0, 2 movement 'Masuk',
  Hutang 530000 jatuh tempo +30 hari, terima kedua diblokir,
  DP>=Total tanpa Hutang), Hutang bayar (sebagian 200000→330000,
  overpayment diblokir, lunas→Lunas), SO (aturan muat, Draft→Diproses→
  Selesai), Piutang Tempo (INV-<id>, sisa 60000, bayar sebagian/lunas/
  overpayment), poin (dapat 98 dari 981000, maks pakai, saldo
  1250−500+98=848), diskon (tertinggi 15% dari 5 periode, nonaktif/
  kadaluarsa/belum-mulai diabaikan), tukar tambah (400000, cap total,
  stok masuk 10→12), history harga (terbaru dulu), konsistensi (csproj,
  sidebar, Designer/code-behind semua kontrol + handler, tanpa gradasi,
  tanpa larangan Guna2), B1 dipertahankan.
- `test_v2_phase1.py`: **61/61 PASS, 0 FAIL** (tidak ada yang rusak;
  suite tidak diubah).
- Cek tambahan: keseimbangan kurung semua file `.cs` OK; tidak ada
  definisi method / field duplikat; semua cross-reference
  (FormStruk ctor baru, MuatSalesOrder, Shell, handler) cocok.
  `KasirControl.cs` dibaca ulang penuh setelah edit; satu perbaikan
  diterapkan saat review: urutan set `numPoinDipakai.Value` sebelum
  `Maximum` (menghindari Value > Maximum sesaat saat ganti pelanggan),
  dan `SetPelanggan` jatuh ke Umum kalau ID pelanggan SO sudah tidak ada
  (supaya diskon/poin tidak nempel ke pelanggan lain).

> **JUJUR: tetap tidak ada compile test** (tidak ada toolchain .NET di
> VM). Pengguna WAJIB Build (Ctrl+Shift+B) di Visual Studio sebelum
> menjalankan. Yang sudah dites: logika SQL + aturan bisnis terhadap
> SQLite asli + konsistensi Designer/code-behind + wiring handler.
> Belum terverifikasi di runtime: tampilan layout baru (baris pelanggan
> Kasir, tab Pembelian/Penjualan, modal baru), perilaku
> `Guna2NumericUpDown` saat Maximum diubah, cetak PO di printer fisik,
> dan bahwa `PrintPreviewDialog` tampil dengan benar.

### 8.8 Sisa risiko / yang sengaja tidak ditutup (Fase 2)

- Race lama dari Guna (didokumentasikan di `GUNA_NOTES.md`) masih ada:
  stok bisa berubah antara `CekStokCukup` dan langkah 1, sehingga
  langkah 1 bisa gagal setelah uang diterima — tapi di Fase 2 ini
  kegagalan langkah 1 terjadi SEBELUM uang diterima (cek stok dulu),
  dan uang baru diterima di FormBayar; kalau langkah 1 gagal setelah
  FormBayar OK, uang sudah diterima dan transaksi batal — stok
  dikembalikan, tapi uang harus dikembalikan manual. Menutup ini butuh
  restrukturisasi alur B1 (di luar Fase 2).
- SO yang dimuat lalu keranjang dikosongkan manual (klik 2x) tetap
  'Diproses' dan bisa dimuat ulang — sesuai spek; tapi tidak ada tombol
  "Batalkan muat SO" eksplisit di Kasir (mengosongkan keranjang adalah
  caranya).
- Hapus pelanggan tidak diblokir kalau pelanggan itu punya SO / Piutang
  (hanya transaksi Kasir yang memblokir, dari Fase 1). SO yatim akan
  tampil tanpa nama pelanggan; `SetPelanggan` jatuh ke Umum saat dimuat.
- Diskon periode hanya berlaku kalau pelanggan dipilih — keputusan
  mengikuti spek tertulis. Kalau seharusnya berlaku juga untuk "Umum",
  ubah `HitungDiskonAktif` (hapus guard `idPelanggan == null`).
- Poin didapat dihitung dari `subtotal − diskon` (poin dipakai & tukar
  tambah tidak mengurangi dasar) — keputusan mengikuti spek ("total
  setelah diskon").
- Nomor PO/SO dari `COUNT(*)` per tahun: aman karena PO/SO tidak pernah
  dihapus (hanya Dibatalkan, nomornya tetap terpakai).

---

## 9. Fase 3 — Konsinyasi + Persediaan & Gudang (8 Nov 2026)

### 9.1 Skema baru (16 tabel + 2 ALTER + seed)

`ScriptSchemaFase3` di `ConnectionManager.cs` menambah 16 tabel (total
menjadi 33):

- **Multi-lokasi:** `Lokasi` (ID_Lokasi PK, Kode UNIQUE, Nama, Alamat,
  IsAktif DEFAULT 1), `StokLokasi` (PK komposit ID_Lokasi + ID_Item,
  Qty REAL).
- **Batch/HPP:** `StokBatch` (ID_Batch PK, ID_Item, ID_Lokasi,
  TanggalMasuk TEXT, QtyAwal, QtySisa, HargaBeli, RefType, RefID),
  `Pengaturan` (Kunci TEXT PK, Nilai TEXT).
- **Konsinyasi Masuk:** `KonsinyasiMasuk` (ID_KSM PK, NoKSM UNIQUE,
  ID_Supplier, Tanggal, Status CHECK IN ('Aktif','Selesai','Dibatalkan'),
  Catatan), `KonsinyasiMasukDetail` (QtyTitipan, QtyTerjual DEFAULT 0,
  HargaTitipan, HargaJual), `KonsinyasiMasukJual` (ID_KMSJ PK,
  ID_KSMDetail, Tanggal, Qty, HargaJual, ID_Transaksi NULL).
- **Konsinyasi Keluar:** `KonsinyasiKeluar` (ID_KSK PK, NoKSK UNIQUE,
  ID_Pelanggan = agen, Status CHECK yang sama), `KonsinyasiKeluarDetail`
  (QtyTitip, QtyTerjual, HargaJual, **HPPUnit REAL** — HPP satuan yang
  dikunci saat barang dititipkan), `KonsinyasiKeluarJual`.
- **Perakitan:** `Rakitan` (ID_Rakitan PK, ID_ItemJadi FK ke Item tipe
  'Rakitan', Nama, Aktif DEFAULT 1), `RakitanDetail` (ID_ItemBahan, Qty
  per unit jadi).
- **Transfer:** `TransferStok` (ID_Transfer PK, NoTransfer UNIQUE,
  ID_LokasiAsal, ID_LokasiTujuan, Status CHECK IN
  ('Draft','Selesai','Dibatalkan')), `TransferDetail` (ID_Item, Qty).
- **Opname:** `Opname` (ID_Opname PK, NoOpname UNIQUE, ID_Lokasi,
  Status CHECK IN ('Draft','Selesai')), `OpnameDetail` (StokSistem,
  StokFisik, Selisih).

ALTER (hanya jika kolom belum ada, dicek lewat PRAGMA table_info):
`StokMovement ADD COLUMN ID_Lokasi INTEGER`,
`Detail_Transaksi ADD COLUMN HPP REAL NOT NULL DEFAULT 0`.

Nomor dokumen: `KSM-/KSK-/TRF-/OPN-` + yyyy + `-` + 4 digit dari
`COUNT(*)` per tahun (sama seperti PO/SO — aman karena dokumen tidak
pernah dihapus, hanya Dibatalkan; nomornya tetap terpakai).

### 9.2 Fondasi multi-lokasi + invarian

- `MigrasiFase3(conn)` dijalankan `EnsureDatabase` setelah migrasi DB
  lama dan seed: seed Lokasi 'GU' / 'GUDANG UTAMA' (jika belum ada),
  seed `Pengaturan HPPMethod = 'FIFO'`, lalu untuk setiap Item
  Fisik/Rakitan dengan Stok > 0: satu baris `StokLokasi` (Lokasi 1,
  Qty = Item.Stok) + satu batch awal (`RefType 'SaldoAwal'`,
  QtyAwal = QtySisa = Stok, HargaBeli = Item.HargaBeli).
  Semua langkah memakai guard `NOT EXISTS` — migrasi idempotent
  (diuji: dijalankan 2x, jumlah baris tidak berubah).
- **Invarian yang dijaga di semua alur:** `Item.Stok ==
  SUM(StokLokasi.Qty)` untuk item Fisik/Rakitan, dan
  `SUM(StokBatch.QtySisa) == StokLokasi.Qty` per (item, lokasi).
  Setiap perubahan stok mengupdate BOTH `Item.Stok` dan `StokLokasi`
  dalam transaksi yang sama. Lokasi default untuk Kasir, terima PO,
  tukar tambah, dan semua alur Fase 1/2 adalah Lokasi 1.
- `StokSesudah` di `StokMovement` sejak Fase 3 berarti **stok lokasi**
  setelah perubahan (untuk movement yang punya ID_Lokasi). Movement
  lama (ID_Lokasi NULL) diperlakukan sebagai lokasi 1 di kartu stok.

### 9.3 HPP FIFO/LIFO + batch (`StokHelper.cs`, baru)

- Helper statis; semua method menerima `(conn, trans)` dan tidak pernah
  membuka koneksi sendiri. `GetHPPMethod` (default 'FIFO', nilai
  tidak valid → 'FIFO'), `SetHPPMethod`, `NaikkanStok` /
  `TurunkanStok` (Item + StokLokasi, guarded, return stok lokasi baru
  atau null), `NaikkanStokLokasi` / `TurunkanStokLokasi` (lokasi saja,
  untuk transfer), `SetStokLokasi`, `BuatBatch`, `KonsumsiBatch`,
  `KembalikanBatch`, `PindahkanBatch`, `TulisMovement`.
- `KonsumsiBatch`: FIFO = `TanggalMasuk ASC, ID_Batch ASC`; LIFO =
  `TanggalMasuk DESC, ID_Batch DESC`. Mengembalikan total HPP +
  rincian per batch. Kalau qty batch tidak cukup, melempar
  `InvalidOperationException` — pemanggil WAJIB rollback (transaksi
  gagal, tidak pernah lanjut diam-diam).
- Setiap stok MASUK membuat batch: terima PO (satu batch per PODetail,
  HargaBeli dari PO), hasil rakitan (HargaBeli = total HPP bahan / qty),
  surplus opname (HargaBeli = Item.HargaBeli), tukar tambah
  (HargaBeli = NilaiTukar / QtyMasuk), kembali konsinyasi keluar
  (HargaBeli = HPPUnit yang tersimpan).
- `Detail_Transaksi.HPP` = total HPP baris itu (HPP satuan × qty), 0
  untuk Jasa/NonInventori — supaya Laporan nanti bisa menampilkan
  laba kotor.

### 9.4 Perubahan pada alur Fase 1/2 (agar invarian terjaga)

- **Kasir step 1** (`KasirControl.cs`): per item kini juga
  `TurunkanStokLokasi` (rollback + pesan kalau stok lokasi tidak
  cukup) dan `KonsumsiBatch` (throw → transaksi step 1 rollback,
  `stokDikurangi` tetap false). `StokSesudah` movement 'Jual' kini
  stok lokasi 1. Step 2: Detail_Transaksi kini menyimpan HPP per baris.
  Tukar tambah: kenaikan stok memakai `NaikkanStok` + `BuatBatch`.
  `CekStokCukup` kini membaca `StokLokasi(1)` (fallback ke Item.Stok).
  `KembalikanStok` kini juga mengembalikan StokLokasi + batch
  (signature berubah: tambah parameter `konsumsiPerItem`).
- **Terima PO** (`PembelianControl.cs`): per PODetail kini
  `NaikkanStok` + `BuatBatch` (HargaBeli dari PO, RefType 'PO',
  RefID = ID_PO); movement 'Masuk' membawa ID_Lokasi.
- **Master Data** (`MasterDataControl.cs`): item baru dengan stok > 0
  kini juga menulis StokLokasi + batch awal (RefType 'StokAwal') dan
  movement membawa ID_Lokasi. Ubah stok: `SetStokLokasi` ke nilai baru
  + batch baru saat naik (RefType 'Penyesuaian') atau konsumsi batch
  saat turun (throw → rollback). Hapus item kini juga menghapus
  StokLokasi, StokBatch, RakitanDetail (sebagai bahan maupun lewat
  Rakitan jadi), Rakitan, KonsinyasiMasukDetail, KonsinyasiKeluarDetail,
  TransferDetail, OpnameDetail untuk item itu.
- Semua string yang dicek suite Fase 1/2 dipertahankan (termasuk
  `"UPDATE Item SET Stok = Stok - @Qty"`, `"VALUES (@ID_Item, @Tanggal,
  'Jual', @Qty"`, `stokDikurangi`, guard diskon/poin/tempo) — kedua
  suite lama lulus tanpa perubahan.

### 9.5 Konsinyasi (`KonsinyasiControl.cs`, baru; 2 tab)

**Konsinyasi Masuk** — barang titipan supplier, BUKAN stok toko:
- Buat KSM: pilih supplier + item (Fisik/Rakitan) + qty titipan +
  HargaTitipan (default HargaBeli) + HargaJual (default HargaJual).
  Stok toko tidak berubah; tidak ada batch; tidak ada Hutang.
- "Catat Penjualan Titipan" (`FormKonsinyasiJual.cs`): validasi
  0 < qty ≤ sisa (QtyTitipan − QtyTerjual). Dalam SATU transaksi:
  QtyTerjual naik (guard SQL `QtyTerjual + @Qty <= QtyTitipan`),
  Transaksi tunai (Bayar = Total, ID_Pelanggan NULL) + Detail
  (HPP = qty × HargaTitipan), StokMovement 'Jual Konsinyasi'
  (Qty = −qty, StokSesudah = Item.Stok **tidak berubah**,
  ID_Lokasi NULL), Hutang (RefType 'KonsinyasiMasuk', RefID = id
  penjualan, Total = Sisa = qty × HargaTitipan, JatuhTempo = tgl jual
  + 30 hari, 'Belum Lunas'). **Hutang hanya dibuat untuk barang yang
  sudah benar-benar terjual.** KSM → 'Selesai' saat semua detail
  terjual penuh. KSM Aktif hanya bisa Dibatalkan kalau belum ada yang
  terjual (guard SQL).
- Movement 'Jual Konsinyasi' **dikecualikan** dari kartu stok (barang
  tidak pernah masuk stok lokasi).

**Konsinyasi Keluar** — barang toko dititipkan ke agen:
- Buat KSK: pilih pelanggan (agen) + item + qty + HargaJual. Dalam
  SATU transaksi: validasi stok Lokasi 1, konsumsi batch (HPP order)
  → **HPPUnit disimpan per detail**, `TurunkanStok`, movement
  'Konsinyasi Keluar' (ID_Lokasi = 1). Stok sudah keluar saat titip.
- "Catat Penjualan Agen" (`FormKonsinyasiJual.cs`): validasi qty ≤
  sisa. Dalam SATU transaksi: QtyTerjual naik, Transaksi tunai +
  Detail (HPP = qty × HPPUnit — dari batch saat titip, bukan harga
  saat ini), movement 'Jual Konsinyasi Keluar' (ID_Lokasi NULL,
  StokSesudah = Item.Stok tidak berubah), Piutang (ID_Pelanggan =
  agen, NoFaktur = 'INV-' + ID_Transaksi, JatuhTempo = tgl jual +
  14 hari, Total = Sisa = qty × HargaJual, 'Belum Lunas').
  **Piutang hanya dibuat saat agen benar-benar menjual.** KSK →
  'Selesai' saat semua detail terjual penuh.
- "Terima Kembali" (`FormKonsinyasiJual.cs`, tanpa kolom harga):
  validasi qty ≤ sisa belum terjual. Dalam SATU transaksi:
  QtyTitip berkurang (guard SQL), `NaikkanStok` + batch baru
  (HargaBeli = HPPUnit, RefType 'KonsinyasiKembali'), movement
  'Konsinyasi Kembali' (ID_Lokasi = 1). KSK tidak punya tombol Batal —
  barang yang tidak terjual dikembalikan lewat Terima Kembali.

### 9.6 Perakitan (`PersediaanControl.cs`, tab Perakitan)

- Resep: pilih item jadi (tipe 'Rakitan') + bahan (item + qty per unit
  jadi). Disimpan di `Rakitan` + `RakitanDetail`.
- "Proses Rakit": pilih resep + qty yang diproduksi. Validasi stok
  bahan di Lokasi 1 cukup — kalau tidak, ditolak dengan pesan yang
  **menyebut nama item bahan yang kurang**. Dalam SATU transaksi:
  bahan turun (StokLokasi + Item.Stok) + konsumsi batch bahan (HPP
  order) dan HPP bahan dijumlahkan, barang jadi naik, batch baru
  untuk barang jadi (QtyAwal = QtySisa = qty, HargaBeli = total HPP
  bahan / qty), movement 'Rakit Keluar' (per bahan) + 'Rakit Masuk'
  (barang jadi).

### 9.7 Transfer antar lokasi (tab Transfer)

- Manajer Lokasi: grid Lokasi + Tambah/Ubah via `FormLokasi.cs`
  (Kode + Nama wajib). Lokasi 'GU' / 'GUDANG UTAMA' sudah ter-seed.
- "Buat Transfer": asal, tujuan, item + qty (boleh banyak baris).
  Draft bisa diedit/dibatalkan. "Selesaikan": dalam SATU transaksi,
  validasi stok asal, kurangi asal + tambah tujuan (Item.Stok total
  **tidak berubah**), pindah batch (batch tertua di asal dulu,
  HargaBeli + TanggalMasuk dipertahankan di tujuan), movement
  'Transfer Keluar' / 'Transfer Masuk' dengan ID_Lokasi set.
  Transfer Selesai terkunci (guard `Status = 'Draft'` saat update).

### 9.8 Stok Opname (tab Opname)

- "Buat Opname": pilih Lokasi → Draft berisi semua item Fisik/Rakitan
  dengan StokSistem terisi dari StokLokasi. User mengisi StokFisik
  per item; Selisih dihitung otomatis (CellValueChanged, dijaga flag
  agar tidak berulang saat memuat).
- "Selesaikan": dalam SATU transaksi, untuk setiap item dengan
  Selisih ≠ 0: StokLokasi.Qty = StokFisik, Item.Stok disesuaikan,
  Selisih < 0 → konsumsi batch sebesar kekurangan; Selisih > 0 →
  batch baru (HargaBeli = Item.HargaBeli); movement 'Opname'
  (Qty = Selisih, StokSesudah = stok baru). Opname Selesai terkunci
  dan hanya bisa dilihat.

### 9.9 Kartu Stok (tab Kartu Stok, read-only)

- Picker item + Lokasi + rentang Dari–Sampai (default tanggal 1 bulan
  ini s.d. hari ini) + "Tampilkan". Membaca StokMovement untuk
  item/lokasi dalam rentang: Saldo Awal = stok lokasi saat ini −
  SUM(Qty gerakan dalam rentang), lalu tiap gerakan dengan Masuk
  (Qty > 0) / Keluar (Qty < 0) dan Sisa berjalan (= StokSesudah),
  plus Stok Akhir. Movement 'Jual Konsinyasi' dan
  'Jual Konsinyasi Keluar' dikecualikan (barang tidak pernah masuk
  stok lokasi). Movement lama (ID_Lokasi NULL) diperlakukan sebagai
  lokasi 1. Tombol "Cetak" memakai PrintPreviewDialog + PrintDocument
  sederhana.

### 9.10 HPP (tab HPP)

- Dropdown FIFO/LIFO disimpan ke `Pengaturan` (HPPMethod) + label
  penjelasan singkat (FIFO = stok tertua keluar dulu; LIFO = stok
  terbaru keluar dulu) + grid read-only batch aktif (QtySisa > 0:
  item, lokasi, tanggal masuk, sisa, harga beli).

### 9.11 UI

- Sidebar `FormMain`: tombol baru `btnNavKonsinyasi` dan
  `btnNavPersediaan` (lazy-loaded seperti kontrol lain, TabIndex
  digeser). `KonsinyasiControl` tabs: Konsinyasi Masuk |
  Konsinyasi Keluar. `PersediaanControl` tabs: Kartu Stok |
  Perakitan | Transfer | Opname | HPP. Modal baru: `FormKonsinyasiJual`
  (dipakai 3 mode: jual titipan, jual agen, terima kembali —
  `TampilkanHarga = false` untuk terima kembali) dan `FormLokasi`.
  Semua form/modal mengikuti gaya flat-solid (panel solid,
  `Guna2Panel` bukan gradient, radius kecil). **Nol gradien di
  seluruh proyek** (dicek test: tidak ada `Guna2GradientPanel`,
  `GradientMode`, `Guna2GradientButton` di file .cs manapun).
- File baru terdaftar di `.csproj`: `StokHelper.cs`,
  `KonsinyasiControl.cs`, `PersediaanControl.cs`,
  `FormKonsinyasiJual.cs`, `FormLokasi.cs`. Referensi
  `libs\Guna.UI2.dll` dan `libs\System.Data.SQLite.dll` tidak berubah.

### 9.12 Verifikasi

- **Suite baru `test_v2_phase3.py`: 101/101 PASS, 0 FAIL**
  (SQLite asli, in-memory; skema diekstrak dari `ConnectionManager.cs`,
  alur bisnis dimodelkan dari SQL yang sama dengan C#).
  - T1 skema: 16 tabel + kolom lengkap + CHECK status menolak nilai
    salah + PK StokLokasi menolak duplikat + NoKSM UNIQUE +
    string migrasi ada di ConnectionManager.
  - T2 migrasi: DB Fase 1/2 lama → stok lama mendarat di Lokasi 1,
    batch awal SaldoAwal benar (QtySisa = stok, HargaBeli =
    HargaBeli), HPPMethod default FIFO, idempotent (2x tidak
    menduplikasi).
  - T3 batch/HPP: FIFO 120 unit = 6.100.000 vs LIFO = 6.500.000
    (beda sesuai harapan, batch tertua/terbaru habis dulu);
    konsumsi melebihi batch ditolak + rollback; HPP Kasir tersimpan
    di Detail_Transaksi (10 × 50.000).
  - T4 konsinyasi masuk: stok toko tidak berubah saat KSM dibuat;
    Hutang hanya untuk yang terjual (20 × 50.000 = 1.000.000);
    jual melebihi sisa ditolak; KSM Selesai saat habis terjual;
    batalkan diblokir kalau sudah ada penjualan, berhasil kalau
    belum ada.
  - T5 konsinyasi keluar: stok keluar saat KSK dibuat (40 → 30),
    HPPUnit = 120.000; Piutang hanya saat agen menjual
    (4 × 150.000, NoFaktur INV-1, jatuh tempo +14 hari); Detail HPP
    = 4 × 120.000; Terima Kembali menaikkan stok + batch baru
    @HPPUnit; KSK Selesai saat seluruh titipan terjual.
  - T6 rakitan: bahan tidak cukup ditolak + stok utuh; rakit 5 →
    semen −10, cat −5, paket 5 → 10, batch jadi HPP 220.000
    ((10×50.000 + 5×120.000)/5).
  - T7 transfer: asal 100 → 80, tujuan 0 → 20, total tetap 100,
    batch ikut pindah; Selesai terkunci; Draft bisa dibatalkan;
    transfer melebihi stok asal ditolak.
  - T8 opname: StokSistem terisi dari StokLokasi; selesai →
    StokLokasi == StokFisik (95/55), selisih negatif konsumsi batch,
    selisih positif bikin batch @HargaBeli; Selesai terkunci.
  - T9 kartu stok: range 03–08 Nov → 3 baris (PO 02 Nov +
    konsinyasi 04 Nov dikecualikan), Saldo Awal 150, Stok Akhir 50,
    running Sisa == StokSesudah (125, 85, 50).
  - T10 invarian: setelah urutan campuran (PO + jual Kasir +
    transfer + opname + rakit + KSK + jual agen + kembali) →
    Item.Stok == SUM(StokLokasi) dan batch == lokasi untuk semua
    item; nilai akhir semen 136 (lok1 116 + lok2 20), besi 45,
    cat 28, paket 7.
  - T11 statis: StokHelper/Kasir/PO/MasterData/Konsinyasi/
    Persediaan berisi string kunci; sidebar + handler ada; csproj
    lengkap; nol gradien; codebuilt checks (regex sama dengan
    suite Fase 2) untuk 4 file baru: semua kontrol dideklarasi,
    semua handler ada (KonsinyasiControl 18, PersediaanControl 28,
    FormKonsinyasiJual 5, FormLokasi 5).
- **Suite lama tidak berubah dan tetap lulus:**
  `test_v2_phase1.py` **61/61 PASS**, `test_v2_phase2.py`
  **111/111 PASS** (dijalankan ulang setelah semua edit Fase 3).
- Selama pengerjaan ada satu insiden edit yang merusak header
  `ScriptSchemaFase2` di ConnectionManager (tertimpa potongan kode
  Kasir) — sudah diperbaiki dan diverifikasi (kurung seimbang,
  kedua const skema ada, tidak ada sisa kode asing).
- **TIDAK ada compile test** (tidak ada toolchain .NET/Mono di VM
  Linux ini, sama seperti Fase 1/2). User HARUS Build
  (Ctrl+Shift+B) di Visual Studio dan mengirim teks error kalau ada.

### 9.13 Sisa risiko / yang sengaja tidak ditutup (Fase 3)

- Belum terverifikasi di runtime: tampilan layout baru (tab
  Konsinyasi/Persediaan, modal FormKonsinyasiJual/FormLokasi),
  perilaku `Guna2NumericUpDown`/`Guna2ComboBox` di form baru,
  dan bahwa `PrintPreviewDialog` kartu stok tampil dengan benar.
- Nomor KSM/KSK/TRF/OPN dari `COUNT(*)` per tahun: aman karena
  dokumen tidak pernah dihapus — tapi kalau dua user membuat dokumen
  yang sama persis bersamaan, nomor bisa bentrok (UNIQUE akan
  menolak salah satunya; tidak ada retry).
- Race lama (Guna/Fase 2) masih ada: stok bisa berubah antara
  `CekStokCukup` dan langkah 1 Kasir. Di Fase 3, langkah 1 kini
  juga memeriksa StokLokasi + batch, jadi kegagalan terdeteksi
  lebih awal — tapi tetap setelah uang diterima kalau FormBayar
  sudah OK (uang harus dikembalikan manual, seperti sebelumnya).
- Konsumsi batch di Kasir memakai `TanggalMasuk` string
  'yyyy-MM-dd HH:mm:ss' — urutan FIFO/LIFO benar selama format
  tanggal konsisten (semua penulis batch memakai format yang sama).
- Kartu stok menghitung Saldo Awal dari stok lokasi saat ini
  dikurangi gerakan dalam rentang — benar selama tidak ada gerakan
  yang terhapus/diubah manual di luar aplikasi.
- Hapus item di Master Data kini menghapus baris-baris Fase 3
  terkait (StokLokasi, batch, resep, konsinyasi, transfer, opname)
  — tapi TIDAK menghapus StokMovement / Detail_Transaksi /
  Hutang/Piutang historis (sengaja: riwayat tetap utuh; item yang
  terhapus akan tampil tanpa nama di riwayat lama).
- Jasa/NonInventori tidak pernah masuk StokLokasi/batch (sesuai
  Fase 1) — kartu stok untuk item Jasa akan kosong.
- Fase 4 (Keuangan) dan Fase 5 (Mobile) belum dimulai.

## Hotfix 8 Nov 2026 — InvalidCastException DataRowView saat klik menu Kasir

- Gejala: klik menu Kasir melempar `System.InvalidCastException:
  'Unable to cast object of type 'System.Data.DataRowView' to type
  'System.IConvertible'.'`
- Akar masalah: di `LoadPelangganCombo()` (KasirControl.cs) urutan
  binding salah — `DataSource` dipasang DULU, baru `DisplayMember` /
  `ValueMember`. Saat `DataSource` dipasang, event
  `SelectedIndexChanged` langsung menyala; karena `ValueMember` belum
  terpasang, `SelectedValue` mengembalikan baris `DataRowView` utuh,
  dan `Convert.ToInt32(...)` di `PerbaruiPelanggan()` melempar.
- Perbaikan: (1) urutan dibalik di SEMUA 21 situs binding combo di
  seluruh proyek (Kasir 1, Pembelian 2, Penjualan 2, Konsinyasi 4,
  Persediaan 10, Master Data 1, FormTukarTambah 1) — `DisplayMember` /
  `ValueMember` dipasang SEBELUM `DataSource`; (2) `PerbaruiPelanggan()`
  kini memakai helper `AmbilIdCombo()` yang tahan `DataRowView`
  (membaca kolomnya langsung) sebagai pengaman kedua.
- Regresi: 2 cek baru T12 di test_v2_phase3.py (urutan binding di
  semua file .cs + keberadaan helper). Suite: Fase 3 103/103, Fase 2
  111/111, Fase 1 61/61, 0 FAIL. Keseimbangan kurung di 7 file yang
  diedit diperiksa OK.
- Masih belum ada compile test — user harus Build (Ctrl+Shift+B) di
  Visual Studio. Zip perbaikan: SistemTokoInventori-v2-fase3.1.zip.

## 10. Fase 4 — Keuangan & Akuntansi (8 Nov 2026)

Keuangan & Akuntansi: deposit/DP, jurnal otomatis, buku besar, neraca,
laba rugi bulanan + YTD. Menu sidebar baru "Keuangan" (tombol ke-9;
sidebar dirapikan ke jarak 48px: Dashboard 116, Kasir 164, Pembelian
212, Penjualan 260, Master Data 308, Laporan 356, Konsinyasi 404,
Persediaan 452, Keuangan 500; lblAdmin tetap 556, Keluar tetap 584).

### 10.1 Skema baru (6 tabel, total 39 tabel)

- `Akun` (ID_Akun PK, Kode UNIQUE, Nama, Jenis CHECK IN
  ('Aktiva','Kewajiban','Modal','Pendapatan','HPP','Biaya'), SaldoNormal
  CHECK IN ('Debit','Kredit'), SaldoAwal REAL DEFAULT 0, IsAktif).
  Seed 19 akun (INSERT OR IGNORE, idempotent): 1101 Kas, 1102 Piutang
  Usaha, 1103 Persediaan, **1104 Uang Muka Pembelian** (Aktiva/Debit —
  akun tambahan di luar daftar brief, supaya jurnal terima PO tetap
  seimbang saat DP > Total), 1201 Aset Tetap, 2101 Hutang Usaha, 2102
  Hutang Konsinyasi, 2103 Deposit Pelanggan, 3101 Modal Pemilik, 3201
  Laba Ditahan, 4101 Pendapatan Penjualan, 4102 Pendapatan Lain-lain,
  5101 Harga Pokok Penjualan, 6101 Biaya Gaji, 6102 Biaya Listrik & Air,
  6103 Biaya Sewa, 6104 Biaya Lain-lain, 6105 Biaya Promosi & Poin,
  6106 Biaya Selisih Persediaan.
- `Jurnal` (ID_Jurnal PK, Tanggal, NoBukti, Keterangan, RefType, RefID)
  + `JurnalDetail` (ID_JurnalDetail PK, ID_Jurnal FK, ID_Akun FK, Debit,
  Kredit).
- `Deposit` (ID_Deposit PK, ID_Pelanggan FK, Tanggal, Jumlah, Sisa,
  Status CHECK IN ('Aktif','Terpakai','Dikembalikan'), Keterangan) +
  `DepositPakai` (ID_DepositPakai PK, ID_Deposit FK, ID_Transaksi FK,
  Tanggal, Jumlah).
- `BiayaOperasional` (ID_Biaya PK, Tanggal, ID_Akun FK, Jumlah,
  Keterangan, NoBukti).

### 10.2 Jurnal otomatis — aturan per alur (JurnalHelper.cs)

`JurnalHelper.TulisJurnal` (satu-satunya penulis jurnal): lewati baris
bernilai 0, tolak nilai negatif, **lempar InvalidOperationException
kalau SUM(Debit) != SUM(Kredit)** (toleransi Rp 0,01), kembalikan 0
kalau semua baris 0 (tidak ada jurnal dibuat). Semua jurnal ditulis
DALAM transaksi yang sama dengan operasi bisnisnya — kalau jurnal
tidak seimbang, operasinya ikut di-rollback (tidak ada jurnal yatim
dan tidak ada operasi tanpa jurnal).

- Penjualan Kasir (langkah 2, setelah Transaksi + Detail tersimpan):
  D Kas [kasMasuk], D Piutang Usaha [sisa piutang], D Deposit Pelanggan
  [deposit dipakai], D Biaya Promosi & Poin [nilai poin], D Persediaan
  [NilaiTukar], K Pendapatan Penjualan [Pendapatan = subtotal − diskon],
  D HPP [total HPP], K Persediaan [total HPP]. NoBukti `INV-<id>`.
  **Bukti seimbang:** kasMasuk + sisaPiutang = Total, dan
  Total + deposit + poin + tukar = Pendapatan, jadi
  Debit = kasMasuk + sisa + deposit + poin + tukar + HPP
        = Pendapatan + HPP = Kredit.
  **Koreksi dari brief:** kasMasuk = Bayar − Kembalian (uang yang
  benar-benar diterima), BUKAN Bayar — kalau pelanggan tunai membayar
  lebih (Bayar > Total), memakai Bayar akan membuat jurnal tidak
  seimbang. FormBayar: Tunai → Kembalian = Bayar − Total; Tempo →
  Kembalian = 0.
- PO Diterima: D Persediaan [Total]; K Kas [DP]; K Hutang Usaha
  [Total − DP kalau > 0]; D Uang Muka Pembelian [DP − Total kalau
  DP > Total]. NoBukti = NoPO. (DP >= Total → tidak ada Hutang.)
- Bayar Hutang: D Hutang Usaha [jumlah] (atau D Hutang Konsinyasi 2102
  kalau RefType hutangnya 'KonsinyasiMasuk'); K Kas [jumlah].
- Terima Piutang: D Kas [jumlah]; K Piutang Usaha [jumlah].
- Jual Konsinyasi Masuk: D Kas [qty × HargaJual]; K Pendapatan
  Penjualan; D HPP [qty × HargaTitipan]; K Hutang Konsinyasi
  [qty × HargaTitipan] — barang milik supplier, jadi HPP-nya lawan
  hutang, bukan persediaan toko.
- Jual agen Konsinyasi Keluar: D Piutang Usaha [qty × HargaJual];
  K Pendapatan Penjualan; D HPP [qty × HPPUnit]; K Persediaan
  [qty × HPPUnit]. Titip keluar sendiri TANPA jurnal (stok sudah keluar
  saat dititipkan; barang masih milik toko).
- Stok Opname selesai: surplus (selisih > 0 × HargaBeli): D Persediaan,
  K Pendapatan Lain-lain; shortfall (selisih < 0, dinilai HPP batch FIFO
  yang terpakai — bukan HargaBeli saat ini): D Biaya Selisih
  Persediaan, K Persediaan. Hanya baris > 0 yang ditulis.
- Perakitan & Transfer: TANPA jurnal (transformasi aset dalam
  Persediaan / antar lokasi).
- Biaya operasional manual: D akun Biaya xxx [jumlah]; K Kas [jumlah].
  NoBukti `BIA-<id>`.
- Deposit terima: D Kas [jumlah]; K Deposit Pelanggan [jumlah]
  (deposit adalah kewajiban, bukan pendapatan).
- Deposit kembali: D Deposit Pelanggan [jumlah]; K Kas [jumlah].

### 10.3 Deposit di Kasir

Input "Deposit dipakai" di samping input poin (KasirControl): label
menampilkan saldo deposit pelanggan; input aktif hanya kalau pelanggan
dipilih DAN punya deposit Aktif dengan Sisa > 0. Maksimal pakai =
min(saldo deposit, total setelah diskon − poin − tukar tambah), supaya
total tidak pernah negatif dan jurnal tetap seimbang. Langkah 2
mengkonsumsi deposit FIFO (tertua dulu): tulis `DepositPakai`,
kurangi `Deposit.Sisa` (Status 'Terpakai' kalau Sisa 0, guard SQL
`WHERE Sisa >= @Jumlah` supaya tidak over-consume), lalu jurnal
penjualan mendebit Deposit Pelanggan. Struk (FormStruk) menampilkan
baris "Deposit dipakai: -Rp X" (konstruktor baru 14 parameter;
konstruktor lama 6 parameter mendelegasikan dengan deposit 0).

### 10.4 KeuanganControl (UserControl baru, 7 tab, dibangun dari kode)

- Jurnal Umum: grid JurnalDetail JOIN Jurnal + Akun, filter rentang
  tanggal + akun, total Debit/Kredit + label "Seimbang" (hijau) /
  "TIDAK SEIMBANG" (merah), Cetak Jurnal (PrintPreviewDialog).
- Buku Besar: pilih akun + periode (default bulan ini) → Saldo Awal
  (SaldoAwal akun + semua gerakan SEBELUM periode — batasnya akhir
  hari sebelum "Dari", supaya gerakan tanggal "Dari" jam 00:00:00
  tetap masuk periode, bukan saldo awal), gerakan dengan running
  balance (Debit-normal: += Debit − Kredit; Kredit-normal:
  += Kredit − Debit), Saldo Akhir.
- Deposit: daftar + Terima Deposit (pelanggan, jumlah, keterangan) +
  Kembalikan (pakai FormCatatPembayaran, tervalidasi terhadap Sisa,
  guard SQL) + Riwayat Pakai (FormDetailPembayaran, dari DepositPakai).
- Biaya: daftar + Tambah Biaya (dropdown akun terbatas Jenis='Biaya').
  **Biaya TIDAK bisa dihapus — hanya tambah** (dipilih yang lebih aman:
  supaya jurnal tidak pernah kehilangan pasangan debit/kreditnya;
  kalau salah input, catat biaya koreksi dengan keterangan jelas).
- Neraca: pilih bulan → Aktiva (Kas, Piutang Usaha, Persediaan, Uang
  Muka Pembelian, Aset Tetap) dan Pasiva (Hutang Usaha, Hutang
  Konsinyasi, Deposit Pelanggan, Modal Pemilik, Laba Ditahan, Laba
  Berjalan) — masing-masing = SaldoAwal + gerakan jurnal bersih sampai
  akhir bulan (bertanda sesuai SaldoNormal). Laba Berjalan = Pendapatan
  − HPP − Biaya sejak 1 Januari sampai akhir periode. Total Aktiva,
  Total Pasiva, Selisih (hijau 0, merah kalau tidak 0). Cetak Neraca.
- Laba Rugi: pilih bulan + toggle Bulanan/YTD (YTD = 1 Jan s.d. akhir
  bulan) → Pendapatan Penjualan, Pendapatan Lain-lain, Total
  Pendapatan, HPP, Laba Kotor, tiap akun Biaya 6101–6106, Total Biaya,
  Laba Bersih (hijau >= 0, merah < 0). Cetak Laba Rugi.
- Saldo Awal: editor Akun.SaldoAwal — hanya akun Aktiva/Kewajiban/
  Modal. Saldo awal BUKAN jurnal; pastikan total Aktiva awal = total
  Kewajiban + Modal awal (neraca awal seimbang), kalau tidak Neraca
  menampilkan selisih.

### 10.5 Batasan yang jujur — tidak ada backfill

**Transaksi Fase 1–3 TIDAK di-backfill ke jurnal.** Jurnal mulai
terbentuk sejak go-live Fase 4. Akibatnya Neraca & Buku Besar =
SaldoAwal akun + jurnal Fase 4 saja. Supaya neraca awal seimbang,
user harus mengisi Saldo Awal (Kas awal, nilai Persediaan awal,
Modal Pemilik, dsb.) di tab Saldo Awal — nilai Persediaan awal
sebaiknya = nilai stok yang ada saat go-live. Transaksi lama tetap
ada di Laporan (omzet, terlaris, kartu stok) seperti biasa; yang
tidak ada hanyalah jurnalnya.

### 10.6 Perubahan kecil lain

- `Theme.cs`: tambah `DangerDark` #7F1D1D (pasangan `SuccessDark`,
  untuk teks merah gelap di label Seimbang/Laba Bersih/Selisih).
- `FormMain`: tombol sidebar ke-9 "Keuangan" (lazy-load seperti tab
  lain). `SistemTokoInventori.csproj`: daftarkan `JurnalHelper.cs`
  dan `KeuanganControl.cs`.
- Perbaikan saat verifikasi (3 bug yang tertangkap sebelum delivery):
  (1) tiga pemanggilan helper di KeuanganControl urutan argumennya
  terbalik (DepositTerima, DepositKembali, Biaya) — diperbaiki ke
  urutan tanda tangan helper; (2) KeuanganControl sempat menambah
  header dua kali (TerapkanTema + InitializeComponent) — yang di
  TerapkanTema dihapus (pola PembelianControl: header di
  InitializeComponent); (3) KeuanganControl memakai
  `reader.GetInt32/GetDecimal/GetString` — diganti
  `Convert.ToInt32/ToDecimal/ToString` mengikuti peringatan
  LaporanControl (System.Data.SQLite mengembalikan Int64/Double).

### 10.7 Verifikasi (test_v2_phase4.py — BARU)

Suite baru memodelkan JurnalHelper + alur bisnis ke SQLite asli dan
menguji: skema + seed 18 akun (idempotent, total 39 tabel); helper
menolak jurnal tidak seimbang / negatif / semua-nol; jurnal tiap
alur (tunai, tempo, komplit poin+tukar+deposit, overpay tunai,
PO tanpa DP / dengan DP / DP > Total, bayar hutang biasa +
konsinyasi, terima piutang, jual KSM, titip KSK tanpa jurnal, jual
KSK, opname surplus + shortfall, biaya, deposit terima/pakai FIFO
2 deposit/kembali); buku besar running balance vs hitung manual;
neraca seimbang (Selisih 0) setelah skenario campuran; laba rugi
bulanan + YTD (bulan kedua membuktikan YTD mengakumulasi);
refund di atas Sisa ditolak. Hasil: **test_v2_phase4 83/83 PASS**,
dan suite lama tetap lulus tanpa perubahan: Fase 1 61/61, Fase 2
111/111, Fase 3 103/103, 0 FAIL. Cek statis: T12 urutan binding
combo hijau di semua file, nol gradient di proyek, csproj +
FormMain + KeuanganControl wiring lengkap, biaya tanpa DELETE.
- Masih belum ada compile test — user harus Build (Ctrl+Shift+B) di
  Visual Studio. Layout tab/tab KeuanganControl + FormMain (9 tombol
  sidebar) belum dicek visual. Zip delivery dibuat terpisah oleh
  main agent (bukan di sini).

## Versi Final — 8 Nov 2026

- Fase 5 (Mobile) DIBATALKAN atas keputusan user ("gak perlu mobile
  deh kayaknya, langsung final aja pakai guna ui").
- Isi final: Fase 1 (shell + DB tunggal + Master Data + barcode) +
  Fase 2 (Pembelian, Penjualan lanjutan, diskon/poin/tempo/tukar
  tambah) + Fase 3 (Konsinyasi, multi-lokasi, HPP FIFO/LIFO,
  perakitan, transfer, opname, kartu stok) + Fase 4 (jurnal otomatis,
  deposit, buku besar, neraca, laba rugi) + hotfix InvalidCastException
  DataRowView di menu Kasir.
- Seluruh UI memakai Guna.UI2.WinForms 2.0.4.6 (DLL dibundel di libs/,
  build offline), gaya modern flat-solid, nol gradien di proyek.
- Verifikasi akhir (dijalankan ulang sebelum pengemasan): Fase 1 61/61,
  Fase 2 111/111, Fase 3 103/103, Fase 4 83/83 — total 358 cek, 0 FAIL.
- Zip final: SistemTokoInventori-v2-FINAL.zip (path relatif, 60 file).
- Langkah pertama setelah extract: extract ke path pendek (mis.
  C:\PL\), cek libs\Guna.UI2.dll ada (~2,1 MB), Unblock DLL kalau
  Windows menandainya, Clean + Rebuild (Ctrl+Shift+B) di Visual Studio.
  Lalu isi Saldo Awal di tab Keuangan supaya neraca awal seimbang
  (jurnal tidak di-backfill dari transaksi Fase 1-3).
- Yang masih terbuka (di luar final ini): P2/P3 bug lama (B5/B6/B11/B13,
  B7/B8/B9/B10), login/roles, backup/export, dan katalog toko bangunan
  (data contoh masih Aqua/Indomie — user belum memutuskan ganti katalog).

## v2 FINAL revisi — 9 Nov 2026: database .db (file nyata di folder aplikasi)

- Alasan: user tidak melihat file .db apa pun — database lama dibuat
  otomatis di %LocalAppData%\SistemTokoInventori\ dan tidak dibundel
  di zip. Atas permintaan user ("pakai file .db aja"), database kini
  berupa file nyata di folder aplikasi, seperti 3 file .db versi lama.
- Perubahan kode (ConnectionManager.cs):
  - DbPath: dari Path.Combine(AppDataDir, "TokoBangunan.db") dengan
    AppDataDir = %LocalAppData%\SistemTokoInventori, menjadi
    Path.Combine(BaseDir, "TokoBangunan.db") dengan BaseDir =
    AppDomain.CurrentDomain.BaseDirectory (folder .exe, bin\Debug).
    AppDataDir dihapus (sudah tidak dipakai di mana pun).
  - EnsureDatabase tetap membuat file kalau belum ada (skema + seed
    dari kode), dan migrasi dari 3 file lama (DB_Produk.db,
    DB_Transaksi.db, DB_Supplier.db di folder aplikasi) tetap jalan.
  - Seed sudah idempotent sebelum revisi ini, jadi tidak ada perbaikan
    guard: ScriptSeedAkun INSERT OR IGNORE; seed Supplier/Item/
    Transaksi dijaga CountRows == 0; MigrasiFase3 memakai guard
    KolomAda + INSERT...SELECT WHERE NOT EXISTS; migrasi file lama
    memakai early-return CountRows > 0 + INSERT OR IGNORE. Menjalankan
    EnsureDatabase di atas .db yang sudah berisi seed TIDAK menggandakan
    baris (19 akun tetap 19 — diverifikasi langsung di .db jadi).
- File TokoBangunan.db jadi dibundel di proyek
  (SistemTokoInventori/TokoBangunan.db, 224 KB): dibangun dari skema +
  seed yang diekstrak langsung dari ConnectionManager.cs — 39 tabel
  (8 Fase 1 + 9 Fase 2 + 16 Fase 3 + 6 Fase 4), seed lengkap (3 supplier,
  4 item contoh, 2 transaksi + 3 detail, 19 akun, Lokasi 'GUDANG UTAMA',
  Pengaturan HPPMethod='FIFO', StokLokasi + StokBatch 'SaldoAwal' untuk
  4 item). PRAGMA integrity_check = ok.
- .csproj: TokoBangunan.db didaftarkan sebagai Content dengan
  CopyToOutputDirectory = PreserveNewest — saat Build, file .db
  tersalin ke bin\Debug di samping .exe. PreserveNewest (BUKAN Always):
  kalau .db di output sudah berisi data transaksi user, Build
  berikutnya TIDAK menimpanya dengan file proyek.
- Verifikasi sebelum pengemasan ulang: keempat suite tes hijau
  (Fase 1 61/61, Fase 2 111/111, Fase 3 103/103, Fase 4 83/83 — total
  358 cek, 0 FAIL), nol gradien di proyek, brace/paren seimbang di
  ConnectionManager.cs, .csproj XML valid.
- Test .py DIHAPUS dari working copy dan delivery folder atas
  permintaan user (test_v2_phase1..4.py). File test lama non-v2
  (test_b1_b12.py, test_p1.py, test_guna_features.py,
  verify_restyle.py) tetap ada di working copy.
- Zip final diperbarui: SistemTokoInventori-v2-FINAL.zip (path relatif)
  kini berisi TokoBangunan.db + 28 file .cs + .csproj + libs/.
- Langkah pertama setelah extract: extract ke path pendek (mis.
  C:\PL\), cek libs\Guna.UI2.dll ada (~2,1 MB) dan
  SistemTokoInventori\TokoBangunan.db ada, Unblock DLL kalau Windows
  menandainya, Clean + Rebuild (Ctrl+Shift+B) di Visual Studio — file
  .db tersalin otomatis ke bin\Debug di samping .exe. Kalau file .db
  dihapus, aplikasi membuatnya ulang saat pertama dibuka. Isi Saldo
  Awal di tab Keuangan supaya neraca awal seimbang (jurnal tidak
  di-backfill dari transaksi Fase 1-3).

## v2 FINAL revisi — 9 Nov 2026: perbaikan judul header (label transparan)

- Bug laporan user (screenshot 9 Nov 2026): judul di bar atas setiap menu
  SELAIN Dashboard tidak terbaca — tulisan putih di atas kotak putih.
  Dashboard tidak kena karena FormMain tidak memakai BuildHeader.
- Penyebab: lblTitle dan lblSub di Theme.BuildHeader() adalah
  System.Windows.Forms.Label biasa dengan BackColor transparan (default).
  Guna2Panel melukis FillColor-nya di OnPaint, bukan OnPaintBackground —
  jadi label transparan di atasnya tidak mendapat warna biru panel,
  melainkan putih. Panelnya sendiri sudah benar (biru #2563EB), teks
  putih di atas biru memang yang benar — jadi yang diperbaiki adalah
  BackColor label, BUKAN ForeColor judul.
- Perbaikan (3 tempat, semua BackColor opak = warna panelnya):
  - SistemTokoInventori/Theme.cs — BuildHeader(): lblTitle.BackColor =
    Primary; lblSub.BackColor = Primary; Ini memperbaiki SEMUA pemanggil
    BuildHeader sekaligus (Kasir, Pembelian, Penjualan, Master Data,
    Laporan, Konsinyasi, Persediaan, Keuangan, FormBayar, FormStruk,
    FormTukarTambah, FormCatatPembayaran, FormDetailPembayaran,
    FormHistoryHarga, FormKonsinyasiJual, FormLokasi).
  - SistemTokoInventori/KasirControl.cs — TerapkanTema():
    lblTotal.BackColor = Theme.SuccessTint; (label "Total: Rp 0" di
    dalam pnlTotal hijau muda, kena bug yang sama).
  - SistemTokoInventori/LaporanControl.cs — TerapkanTema():
    lblTotalOmzet.BackColor = Theme.SuccessTint; (label omzet di dalam
    pnlTotalOmzet hijau muda, kena bug yang sama).
- Sapu bersih: grep seluruh copy — hanya 3 situs Guna2Panel berwarna
  (BuildHeader Primary + 2 SuccessTint); semuanya sudah ber-BackColor
  opak. Panel lain: FormMain.pnlSidebar adalah Panel biasa (BackColor
  Sidebar, melukis normal — judul sidebar terbaca), kartu KPI/StyleCard
  dan pnlStok pakai FillColor putih (Card) sehingga label transparan di
  atasnya tidak menimbulkan kotak putih. Label-label di TabPage dan form
  duduk di atas background biasa — tidak kena bug ini.
- Verifikasi: grep BackColor = Primary ada di BuildHeader untuk kedua
  label; BackColor = SuccessTint ada di KasirControl (lblTotal) dan
  LaporanControl (lblTotalOmzet); brace/paren/bracket seimbang di
  Theme.cs, KasirControl.cs, LaporanControl.cs; tetap NOL gradien di
  proyek (tidak ada Guna2GradientPanel / gradient baru). File test .py
  sudah dihapus atas permintaan user dan TIDAK dibuat ulang — verifikasi
  dilakukan dengan grep + cek keseimbangan kurung, bukan suite tes.
- Zip final diperbarui: SistemTokoInventori-v2-FINAL.zip (path relatif)
  berisi Theme.cs yang sudah diperbaiki; TIDAK ADA file test .py di
  dalam zip (4 file test lama non-v2 yang masih ada di working copy
  juga tidak ikut dikemas).
- Langkah untuk user: extract ke path pendek, Clean + Rebuild
  (Ctrl+Shift+B) di Visual Studio, lalu buka tiap menu — judul harus
  putih terbaca di atas bar biru, dan "Total: Rp 0" di Kasir serta
  "Total Omzet" di Laporan harus terbaca hijau gelap di atas hijau muda.
