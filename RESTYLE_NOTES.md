# Restyle UI — Layout A (sidebar + dashboard, palet biru)

Tanggal: 8 Nov 2026. Palet: primary #2563EB, sidebar #1E293B, background #F1F5F9,
kartu putih, teks #0F172A, muted #64748B, sukses #16A34A, bahaya #DC2626,
peringatan #D97706, teal #0D9488. Font: Segoe UI di semua tempat.
Tanpa library pihak ketiga, tetap .NET Framework 4.7.2.

## File yang diubah / ditambah

| File | Perubahan |
|---|---|
| `SistemTokoInventori/Theme.cs` | **BARU.** Warna palet + helper: `StylePrimaryButton`, `StyleSuccessButton`, `StyleDangerButton`, `StyleGhostButton`, `StyleNavButton`, `StyleGrid`, `StyleForm`, `BuildHeader` (panel header biru Dock=Top 64px), `StyleCard` (kartu putih + strip aksen 4px). |
| `SistemTokoInventori/SistemTokoInventori.csproj` | Tambah `<Compile Include="Theme.cs" />`. |
| `SistemTokoInventori/FormMain.cs` | **Rewrite.** Sidebar + dashboard. Lihat di bawah. |
| `SistemTokoInventori/FormMain.Designer.cs` | **Rewrite.** Sidebar (210px) + area utama, 4 kartu KPI, grid Produk Terlaris, panel Stok Menipis, status DB. |
| `SistemTokoInventori/FormKasir.cs` | Hanya tambah constructor `TerapkanTema()` (tema + header). Logika cart/transaksi **tidak diubah** (termasuk perbaikan B1). |
| `SistemTokoInventori/FormKasir.Designer.cs` | **Rewrite layout.** Header 64px, grid di-anchor mengisi form, kartu total, tombol Proses 180x60. Semua nama kontrol & handler dipertahankan. |
| `SistemTokoInventori/FormInventori.cs` | Tambah `TerapkanTema()` + `LoadSupplierMap()`; `LoadProduk()` menambah kolom "Supplier" (nama) dan menyembunyikan `ID_Supplier`. Logika CRUD **tidak diubah**. |
| `SistemTokoInventori/FormInventori.Designer.cs` | **Rewrite layout.** Grid di-anchor mengisi tab, field/button dirapikan, font Segoe UI 10. |
| `SistemTokoInventori/FormLaporan.cs` | Hanya tambah constructor `TerapkanTema()` (tema + header + kartu total). Logika laporan **tidak diubah**. |
| `SistemTokoInventori/FormLaporan.Designer.cs` | **Rewrite layout.** Kartu Total Omzet, grid di-anchor mengisi form. |

## FormMain — dashboard (baru)

- Sidebar gelap kiri (210px): judul "Sistem Toko Inventori", nav Dashboard / Kasir /
  Inventori / Laporan (yang aktif berlatarkan biru), "Admin Toko" + tombol "Keluar"
  di bawah. **Tidak ada tombol Pengaturan** — tidak ada fitur pengaturan, tombol mati
  akan jadi UI palsu.
- Sapaan "Selamat datang, Admin" + tanggal hari ini format Indonesia (id-ID).
- 4 kartu KPI (strip aksen atas): Omzet Hari Ini, Total Produk, Transaksi Hari Ini,
  Stok Menipis (angka merah kalau > 0).
- Produk Terlaris: 5 besar, tabel Nama_Produk / Terjual / Omzet.
- Stok Menipis: daftar produk stok <= 10 + tombol "Lihat di Inventori" (membuka
  FormInventori, lalu dashboard di-refresh).
- Indikator "Database OK ✓" / "Database Error!" tetap di kiri bawah area utama
  (memakai `EnsureDatabases` + `TestAllConnections` yang sudah ada).
- Tombol Dashboard me-refresh KPI; setelah Kasir/Inventori/Laporan ditutup, nav
  kembali ke Dashboard dan KPI di-refresh.

## Query dashboard (FormMain.cs)

- Omzet Hari Ini: `SELECT COALESCE(SUM(Total), 0) FROM Transaksi WHERE Tanggal LIKE @Hari`
  (`@Hari` = `yyyy-MM-dd%`, karena Tanggal disimpan sebagai teks "yyyy-MM-dd HH:mm:ss").
- Transaksi Hari Ini: `SELECT COUNT(*) FROM Transaksi WHERE Tanggal LIKE @Hari`.
- Total Produk: `SELECT COUNT(*) FROM Produk`.
- Stok Menipis: `SELECT COUNT(*) FROM Produk WHERE Stok <= @Batas`, konstanta
  `BatasStokMenipis = 10`.
- Produk Terlaris: `SELECT ID_Produk, SUM(Qty), SUM(Subtotal) FROM Detail_Transaksi
  GROUP BY ID_Produk` + nama produk dari DB_Produk (JOIN manual seperti FormLaporan),
  diurutkan `Total_Terjual DESC, Total_Omzet DESC`, diambil 5 besar. Produk yang sudah
  dihapus dilewati (detail yatim).
- Daftar Stok Menipis: `SELECT Nama_Produk, Stok FROM Produk WHERE Stok <= @Batas
  ORDER BY Stok ASC`. Kalau kosong: "Semua stok aman ✓".

## FormInventori — kolom Supplier

`LoadProduk()` memuat peta ID_Supplier → Nama_Supplier dari DB_Supplier, menambah
kolom "Supplier" ke DataTable, dan menyembunyikan kolom `ID_Supplier`
(`Visible = false`). Kolom ID tetap ada dan tetap dibaca `dgvProduk_CellClick`
untuk mengisi `cmbSupplier.SelectedValue`, jadi update produk tetap jalan.

## Penyimpangan dari brief (disengaja, dicatat)

1. Header panel dibuat di code-behind lewat `Theme.BuildHeader()` (bukan di Designer),
   supaya warna header terpusat di Theme.cs. Panel Dock=Top ditambahkan setelah
   `InitializeComponent()`; kontrol lain diposisikan mulai y=76 (Kasir/Laporan) dan
   tabControl Dock=Fill menyusut otomatis (Inventori). Urutan docking WinForms
   (kontrol yang ditambahkan terakhir di-dock lebih dulu) membuat ini bekerja.
2. `btnProses` di FormKasir di-override tingginya ke 60px setelah
   `StyleSuccessButton` (default 42px), supaya tombol utama lebih besar — sesuai
   mockup.
3. Label judul kartu KPI di FormMain memakai strip aksen `Theme.StyleCard`
   (panel 4px Dock=Top di dalam kartu), bukan border — WinForms Panel tidak punya
   border berwarna.
4. `FormLaporan` dan `FormKasir` tidak memakai `MinimumSize` baru yang ekstrem:
   Kasir 860x580, Laporan/Inventori 860x560, FormMain 1000x600 — supaya grid
   yang di-anchor tidak menumpuk field saat form diperkecil.

## Yang TIDAK diubah

- Perbaikan P0 sebelumnya tetap ada: flag `stokDikurangi` + `KembalikanStok`
  transaksional di FormKasir.cs, dan `Busy Timeout=5000` di ConnectionManager.cs.
- Semua logika bisnis (cart, stok, CRUD, validasi, laporan) tidak diubah, hanya
  pemanggilan tema di constructor dan — khusus Inventori — kolom Supplier.
- Tidak ada push ke GitHub; perubahan hanya lokal di repo ini.

## Verifikasi yang dilakukan (tanpa compiler)

Tidak ada toolchain .NET/Mono di VM ini, jadi **belum ada compile test**.
Yang dilakukan sebagai gantinya:

1. `verify_restyle.py` (di repo ini) — **semua check lulus**: keseimbangan `{}`/`()`
   di 9 file .cs; setiap event handler yang di-wire di Designer ada di code-behind;
   setiap kontrol yang dipakai code-behind dideklarasikan di Designer; tidak ada
   deklarasi kontrol duplikat; tidak ada referensi ke handler lama FormMain
   (`btnKasir_Click` dkk.); Theme.cs terdaftar di .csproj; setiap `Theme.X` yang
   dipakai ada definisinya; perbaikan P0 (B1/B12) masih ada.
2. Re-read end-to-end semua file yang diedit: Theme.cs, FormMain(.cs + Designer),
   FormKasir(.cs + Designer), FormInventori(.cs + Designer), FormLaporan(.cs +
   Designer), dan diff `.csproj`. Tidak ada handler/logika yang terhapus atau
   diganti nama; semua nama kontrol di code-behind cocok dengan Designer.

**Wajib sebelum dipakai: Build (Ctrl+Shift+B) di Visual Studio.**
Kalau Build menemukan error, kirim pesan error-nya.
