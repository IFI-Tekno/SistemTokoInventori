# Guna UI Migration + 3 Fitur — Catatan

Proyek: SistemTokoInventori (.NET Framework 4.7.2, WinForms).
Dikerjakan HANYA di copy ini (`~/workspace/sistem-toko-inventori-guna`); copy
`~/workspace/sistem-toko-inventori-review` (versi Theme.cs) TIDAK disentuh dan
tetap jadi fallback. Tidak ada yang di-push ke GitHub.

## 0. Yang TIDAK bisa diverifikasi di VM ini (baca dulu)

**Tidak ada compile test.** VM Linux ini tidak punya toolchain .NET/Mono
(`apt-get update` macet, tidak ada package lists). Semua pengecekan di bawah
adalah pembacaan kode + test Python yang memodelkan logika C#-nya, BUKAN build.
**User harus Build (Ctrl+Shift+B) di Visual Studio** dan memperbaiki error
compile kalau ada — kemungkinan besar tidak ada, tapi tidak bisa dijamin
tanpa compile.

**Lisensi Guna:** Guna.UI2.WinForms adalah produk komersial (EULA: activation
code per developer, langganan tahunan berbayar, tanpa royalti untuk aplikasi
yang didistribusikan). **User harus menerima/mengaktifkan lisensi Guna di
Visual Studio** saat pertama build. Kalau tidak mau aktivasi, pakai versi
Theme.cs di folder review sebagai fallback.

## 1. DLL dibundel (seperti SQLite)

- `SistemTokoInventori/libs/Guna.UI2.dll` (2.161.544 byte, dari paket
  Guna.UI2.WinForms **2.0.4.6**, `lib/net472/Guna.UI2.dll` — cocok dengan
  target framework proyek). md5 sama dengan file sumber di `/tmp/guna_dll/`.
- `SistemTokoInventori.csproj`: `<Reference Include="Guna.UI2">` dengan
  `<HintPath>libs\Guna.UI2.dll</HintPath>` + `<Private>True</Private>`,
  persis seperti referensi System.Data.SQLite. **Tidak pakai packages.config**
  — build tetap offline-safe; DLL ikut tersalin ke `bin\Debug` saat build.
- Semua nama tipe + properti Guna2 yang dipakai sudah dikonfirmasi ADA di DLL
  dengan memeriksa metadata .NET-nya (bukan dari ingatan): Guna2Button,
  Guna2DataGridView, Guna2TextBox, Guna2ComboBox, Guna2NumericUpDown,
  Guna2TabControl, Guna2DateTimePicker, Guna2Panel, Guna2GradientPanel,
  Guna2Elipse — plus properti BorderRadius, FillColor, FillColor2,
  GradientMode, ThemeStyle/HeaderStyle/RowsStyle/AlternatingRowsStyle,
  PlaceholderText, FocusedState, HoverState, TabMenuBackColor,
  TabButtonIdleState/HoverState/SelectedState, UpDownButtonFillColor,
  TargetControl, Value/Minimum/Maximum, Format, Multiline, ReadOnly,
  ScrollBars, TextAlign.

## 2. Part A — Migrasi Guna UI

### Pemetaan kontrol (nama kontrol, event handler, dan logika bisnis TIDAK berubah)

| Standar | Guna2 | Keterangan |
|---|---|---|
| Button | Guna2Button | BorderRadius 10, FillColor dari palet, HoverState.FillColor |
| DataGridView | Guna2DataGridView | ThemeStyle: header Sidebar #1E293B + teks putih bold, baris selang-seling #F8FAFC, seleksi Primary |
| TextBox | Guna2TextBox | BorderRadius 8, PlaceholderText, FocusedState.BorderColor = Primary |
| ComboBox | Guna2ComboBox | BorderRadius 8, FocusedState.BorderColor = Primary |
| NumericUpDown | Guna2NumericUpDown | BorderRadius 8, UpDownButtonFillColor = Primary |
| TabControl | Guna2TabControl | TabMenuBackColor Sidebar, tab terpilih Primary |
| DateTimePicker | Guna2DateTimePicker | BorderRadius 8, Format Short |
| Panel (kartu KPI, pnlStok, pnlTotal, pnlTotalOmzet) | Guna2Panel | BorderRadius 12, FillColor |
| — (baru) | Guna2Elipse | BorderRadius 12 di tiap form (disimpan di field `formElipse`) |
| Header (dulu Panel biasa) | Guna2GradientPanel | Gradasi Primary → PrimaryDark, horizontal |

Panel yang TIDAK diganti: `pnlSidebar`, `pnlMain` (tetap Panel standar —
sudut membulat di sidebar/full-height tidak cocok). Label, ListBox, TabPage
tetap standar.

### Temuan penting saat migrasi (kenapa ada perubahan kecil di code-behind)

Metadata DLL membuktikan **tidak semua Guna2 mewarisi kontrol standar yang
namanya mirip** — jadi beberapa hal tidak bisa sekadar ganti tipe:

- `Guna2Button` mewarisi `Control` (BUKAN `Button`): tidak ada
  `FlatStyle`/`UseVisualStyleBackColor`. Semua baris
  `UseVisualStyleBackColor = true;` dihapus dari Designer. Helper
  `Theme.Style*Button` / `StyleNavButton` / `FormMain.SetActiveNav` sekarang
  menerima `Guna2Button`, bukan `Button`.
- `Guna2TextBox` mewarisi `UserControl` (BUKAN `TextBox`): tidak ada jaminan
  method `Clear()`. `ClearProdukForm()` / `ClearSupplierForm()` di
  FormInventori diubah jadi `txtX.Text = ""`. (`.Text`, `.Focus()`,
  `.PlaceholderText` tetap ada.)
- `Guna2NumericUpDown` mewarisi `UserControl` (BUKAN `NumericUpDown`):
  tidak mengimplementasikan `ISupportInitialize`, jadi baris
  `BeginInit()`/`EndInit()` untuk `numQty` dihapus dari Designer
  (kalau tidak, `InvalidCastException` saat `InitializeComponent`).
  Properti `Value`/`Minimum`/`Maximum` (decimal) tetap ada — baris
  `numQty.Minimum = 1` (B3) tidak berubah.
- `Guna2DateTimePicker` mewarisi `Control` (BUKAN `DateTimePicker`):
  properti `Value`/`Format`/`MinDate`/`MaxDate` tetap ada.
- Yang BENAR-BENAR mewarisi standar: `Guna2DataGridView` → DataGridView,
  `Guna2ComboBox` → ComboBox, `Guna2TabControl` → TabControl,
  `Guna2Panel`/`Guna2GradientPanel` → Panel, `Guna2Elipse` → Component.
- Enum/handler standar TIDAK diganti: `DataGridViewSelectionMode`,
  `DataGridViewCellEventHandler` tetap `System.Windows.Forms.*`
  (sed awal sempat salah menggantinya jadi `Guna2...`, sudah diperbaiki
  dan diverifikasi 0 sisa).

### Theme.cs — tetap sumber palet

Warna TIDAK berubah: #2563EB / #1E293B / #F1F5F9 / #16A34A / #DC2626 /
#D97706. Helper diadaptasi ke properti Guna2 (`FillColor`, `BorderRadius`,
`HoverState`, `ThemeStyle`, `TabButton*State`, `FocusedState`) sebagai
pengganti `FlatStyle`/`FlatAppearance`. Helper baru: `StyleTextBox`,
`StyleComboBox`, `StyleNumericUpDown`, `StyleTabControl`, `StyleDatePicker`,
`ApplyRoundedForm` (mengembalikan Guna2Elipse). `BuildHeader` sekarang
mengembalikan `Guna2GradientPanel`. `StyleCard` tetap menerima `Panel`
(Guna2Panel IS-A Panel) dan membulatkan sudut kalau kartunya Guna2Panel.
Layout sidebar + dashboard FormMain TIDAK diubah.

### P0 + P1 tetap utuh

B1 (flag `stokDikurangi`, `KembalikanStok` transaksional), B12 (Busy
Timeout=5000), B2 (CurrentCulture dulu baru InvariantCulture), B3
(`numQty.Minimum = 1` + guard `qty <= 0`), B4 (blokir hapus produk
ber-riwayat) — semuanya tidak disentuh. Lihat hasil test di bawah.

## 3. Part B — 3 Fitur Baru

### B1. Bayar tunai + kembalian + struk

- **FormBayar.cs + FormBayar.Designer.cs (BARU)**: dialog modal. Isi: Total
  (besar), input "Uang Diterima" (Guna2TextBox, placeholder "Contoh: 50000"),
  "Kembalian" live = Bayar − Total, label peringatan merah, tombol
  "Konfirmasi Bayar" + "Batal". Parse uang memakai urutan yang sama seperti
  B2 (CurrentCulture id-ID dulu, Invariant fallback — "50.000" = 50000).
  **Tombol konfirmasi nonaktif selama Bayar < Total** (atau input tidak
  valid); ada pengaman kedua di `btnKonfirmasi_Click` yang menolak lagi
  kalau `bayar < Total`.
- **Alur di FormKasir.btnProses_Click** (logika B1 tidak berubah):
  keranjang kosong → tolak → salin snapshot + hitung total →
  **`CekStokCukup(snapshot)` dulu** (cek stok di DB sebelum uang diterima,
  supaya tidak sampai uang sudah masuk tapi transaksi batal karena stok) →
  FormBayar (batal → `return`, tidak ada yang berubah) → langkah 1 kurangi
  stok (flag `stokDikurangi` seperti semula) → langkah 2 INSERT Transaksi +
  Detail_Transaksi → clear keranjang → **FormStruk**.
- **Kolom baru Transaksi**: `Bayar REAL NOT NULL DEFAULT 0`,
  `Kembalian REAL NOT NULL DEFAULT 0`. Skema baru di `ScriptTransaksi`
  SUDAH berisi keduanya (DB baru langsung benar). Untuk **DB lama**:
  `ConnectionManager.EnsureTransaksiColumns()` dipanggil dari
  `EnsureDatabases()` — cek `PRAGMA table_info(Transaksi)` dulu, `ALTER
  TABLE ADD COLUMN` HANYA kalau kolom belum ada, tidak pernah DROP/CREATE
  ulang, data lama utuh (Bayar/Kembalian lama = 0). INSERT langkah 2
  sekarang menyimpan Bayar + Kembalian.
- **FormStruk.cs + FormStruk.Designer.cs (BARU)**: struk tampil (tanpa
  cetak) di Guna2TextBox multiline read-only font Consolas: nama toko
  "Sistem Toko Inventori", No. Transaksi, tanggal (id-ID), satu baris per
  item (`nama`, `qty x harga = subtotal`), Total, Bayar, Kembalian, tombol
  "Tutup". MessageBox "Transaksi berhasil!" diganti struk ini.
- **Jalur yang membuat transaksi tidak bisa tercatat saat Bayar < Total:**
  (1) tombol Konfirmasi di FormBayar nonaktif + ada guard di click handler,
  (2) `FormKasir` hanya lanjut ke INSERT kalau `ShowDialog() == OK`,
  (3) `Bayar`/`Kembalian` yang disimpan berasal dari FormBayar yang sudah
  lolos guard itu. Catatan jujur: masih ada celah race yang SANGAT kecil —
  kalau stok berubah DI ANTARA `CekStokCukup` dan langkah 1 (sesi lain
  membeli persis di saat itu), langkah 1 tetap bisa batal SETELAH uang
  diterima; uangnya harus dikembalikan manual. Ini keterbatasan struktur
  2-langkah yang sudah ada (B1), bukan bug baru — dan jendelanya jauh lebih
  kecil daripada sebelum ada `CekStokCukup`.

### B2. Pencarian (search)

Guna2TextBox + PlaceholderText di 4 tempat, filter `DataView.RowFilter`
pada tabel yang SUDAH dimuat (tanpa query ulang); kotak dikosongkan →
semua baris kembali:

| Form | Kotak | Filter |
|---|---|---|
| FormKasir | `txtCari` ("Cari produk...") | `Nama_Produk LIKE '%...%'` |
| FormInventori tab Produk | `txtCariProduk` ("Cari produk...") | `Nama_Produk LIKE '%...%'` |
| FormInventori tab Supplier | `txtCariSupplier` ("Cari supplier...") | `Nama_Supplier LIKE '%...%'` |
| FormLaporan | `txtCari` ("Cari produk...") | `Nama_Produk LIKE '%...%'` |

Escaping: `'` digandakan, wildcard LIKE (`%`, `_`, `[`) di-escape
(`[%]`, `[_]`, `[[]`) — input seperti `O'Brien`, `100%`, `A_B`, `[Pedas]`
tidak merusak sintaks RowFilter. Bonus kecil: `txtHarga` /
`txtStok` di FormInventori diberi PlaceholderText ("Contoh: 3500" /
"Contoh: 100").

### B3. Filter tanggal di FormLaporan

- `dtpDari` + `dtpSampai` (Guna2DateTimePicker, Format Short), default =
  **awal bulan ini .. hari ini**; tombol "Terapkan" (primary).
- Query rekap (`produkData`) memakai `INNER JOIN Transaksi` +
  `WHERE t.Tanggal BETWEEN @Dari AND @Sampai` dengan parameter
  `'yyyy-MM-dd 00:00:00'` s.d. `'yyyy-MM-dd 23:59:59'` — satu hari penuh,
  jadi transaksi 23:59:59 di tanggal Sampai ikut terhitung, 00:00:01
  keesokan harinya tidak. Total Omzet + grid ikut rentang yang sama.
  Produk yang tidak terjual di rentang itu tetap tampil dengan 0 (sama
  seperti perilaku laporan sebelumnya).
- Validasi: Dari > Sampai ditolak dengan pesan.
- **"Load Laporan" = tampilkan semua**: mereset `filterDari`/`filterSampai`
  ke null, mengosongkan pencarian, mengembalikan picker ke default, lalu
  `LoadLaporan()`.

### Yang sengaja TIDAK ditambahkan (sesuai tugas)

Login/roles, backup/export, diskon/pajak/barcode — butuh keputusan desain,
nanti. Juga tidak diubah: B5–B13 (P2/P3), kecuali B2/B3/B4 yang sudah ada.

## 4. File yang berubah / baru

- Diubah: `SistemTokoInventori.csproj`, `Theme.cs`,
  `ConnectionManager.cs`, `FormMain.cs`, `FormKasir.cs`,
  `FormInventori.cs`, `FormLaporan.cs`, ke-4 `*.Designer.cs`.
- Baru: `FormBayar.cs`, `FormBayar.Designer.cs`, `FormStruk.cs`,
  `FormStruk.Designer.cs`, `libs/Guna.UI2.dll`, `test_guna_features.py`,
  file ini (`GUNA_NOTES.md`).
- Tidak disentuh: `~/workspace/sistem-toko-inventori-review/` (fallback),
  `Program.cs`, `Properties/*`, `App.config`, `README.md`,
  `RESTYLE_NOTES.md`.

## 5. Verifikasi (semua di VM ini, tanpa compile)

### Test script — semua LULUS

| Script | Hasil |
|---|---|
| `python3 test_b1_b12.py` | **12/12 PASS** (S1–S5 + static B1/B12) |
| `python3 test_p1.py` | **29/29 PASS** (B2 parse, B3 guard, B4 gate + static) |
| `python3 verify_restyle.py` | **173/173 PASS** (balance, handler, kontrol, Theme, P0) |
| `python3 test_guna_features.py` (BARU) | **51/51 PASS** (lihat bawah) |

`verify_restyle.py` diadaptasi untuk migrasi (BUKAN dilemahkan): regex
deklarasi juga menerima `Guna.UI2.WinForms.Guna2*`, helper Theme yang
mengembalikan tipe Guna2 ikut ditangkap, prefix `dtp` ikut dicek, dan
FormBayar/FormStruk ikut diperiksa. Isi check-nya sama.

### test_guna_features.py — 51 check, memodelkan logika C# persis

- **Payment math**: bayar 50000/total 35000 → kembalian 15000 + confirm
  aktif; bayar pas → kembalian 0; bayar 20000/kosong/"abc" → diblokir +
  warning; "50.000" (id-ID) → 50000. Transaksi tidak bisa tercatat saat
  bayar < total.
- **Date-range SQL + migrasi, di SQLite beneran**: DB skema LAMA (tanpa
  Bayar/Kembalian) → migrasi menambah kedua kolom, data lama utuh
  (5 transaksi, total 163000), default 0, jalan kedua no-op. INSERT baru
  menyimpan (35000/50000/15000). Filter 05–08 Nov: produk 1 terjual 19 /
  omzet 70000, produk 2 terjual 8 / 29000, total rentang 138000; filter
  08 Nov saja = 50000 (transaksi 23:59:59 ikut, 00:00:01 tgl 9 tidak);
  tanpa filter = 198000.
- **Search**: "indo"→Indomie, case-insensitive, kosong→semua,
  `O'Brien`/`100%`/`A_B`/`[Pedas]` cocok literal (escaping benar).
- **Static**: referensi csproj + DLL ada, INSERT berisi Bayar/Kembalian,
  FormBayar dibuka SEBELUM INSERT + batal → return, `CekStokCukup`
  sebelum FormBayar, guard `bayar < Total` di FormBayar, migrasi cek
  PRAGMA + ALTER bersyarat + tanpa DROP, filter BETWEEN + hari penuh,
  reset Load Laporan, struk berisi nama toko/Total/Bayar/Kembalian +
  loop item.

### Pengecekan manual (baca ulang kode)

- Semua file yang diedit dibaca ulang end-to-end: Theme.cs, FormMain.cs,
  FormKasir.cs, FormInventori.cs, FormLaporan.cs, ConnectionManager.cs,
  ke-4 Designer, FormBayar.cs/.Designer.cs, FormStruk.cs/.Designer.cs,
  csproj (XML valid).
- Diff terhadap copy review: hanya perubahan yang diniatkan (swap tipe,
  hapus `UseVisualStyleBackColor`/`BeginInit numQty`, pindah posisi grid,
  kontrol baru, referensi csproj). Tidak ada kontrol/handler yang hilang.
- Jalur pembayaran ditelusuri: FormBayar gate → CekStokCukup → INSERT
  Bayar/Kembalian → FormStruk; batal dari FormBayar tidak menyentuh stok
  maupun transaksi.
- Satu temuan saat verifikasi: dua ekspektasi angka di
  `test_guna_features.py` sempat salah hitung (total lama 165000 → 163000;
  tanpa filter 172000 → 198000) — itu salah hitung di TEST, bukan di kode
  aplikasi; sudah diperbaiki dan test lulus penuh.

## 6. Yang harus dilakukan user di Visual Studio

1. Buka `SistemTokoInventori.sln` dari folder copy ini.
2. **Build (Ctrl+Shift+B).** Kalau ada error compile terkait Guna
   (mis. nama properti), laporkan pesan error persisnya — kemungkinan
   besar tidak ada karena properti sudah dikonfirmasi dari metadata DLL.
3. **Terima/aktifkan lisensi Guna** saat diminta Visual Studio
   (activation code per developer, langganan tahunan).
4. Jalankan, coba: bayar tunai kurang dari total (harus diblokir), bayar
   pas/lebih (struk muncul, kembalian benar), cari produk di Kasir/
   Inventori/Laporan, filter tanggal di Laporan, lalu klik Load Laporan
   (kembali tampil semua).
5. Kalau database lama dipakai, kolom Bayar/Kembalian ditambahkan otomatis
   saat aplikasi pertama jalan — tidak perlu hapus file .db.
