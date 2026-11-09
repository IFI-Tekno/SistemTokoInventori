# Sistem Toko Inventori

Aplikasi Windows Forms .NET Framework 4.7.2 dengan **3 database SQLite terpisah**
(Produk, Transaksi, Supplier) untuk tugas Pemrograman Lanjut.

## Setup (tanpa internet — dll sudah dibundel)

1. **Tutup Visual Studio** kalau sedang terbuka.
2. **Clone / ekstrak repo ini ke folder BARU yang bersih.**
   Jangan ditimpa ke project lama yang masih menyisakan `packages`, `bin`, `obj`, atau `.vs`.
3. Buka `SistemTokoInventori.sln`.
4. **Build** (`Ctrl+Shift+B`) — tidak perlu Restore NuGet, tidak perlu internet.
   - Reference `System.Data.SQLite` menunjuk ke `libs\System.Data.SQLite.dll` yang ada di repo.
   - `SQLite.Interop.dll` (x86 & x64) otomatis ter-copy ke output saat Build.
5. F5. Saat pertama jalan, 3 file `.db` + tabel + data contoh dibuat otomatis di `bin\Debug`.

> Kalau kamu memaksa memakai project lama: tutup VS, hapus folder `.vs`, `bin`, `obj`,
> `packages` dan file `packages.config` di project lama, lalu salin isi repo ini menggantikannya.
> Lebih bersih: clone ke folder baru.

## Fitur

- **FormMain:** Menu utama (Kasir, Inventori, Laporan) + indikator status database.
- **FormKasir:** Transaksi penjualan — pilih produk, qty, keranjang (produk sama digabung),
  validasi stok, proses transaksi dengan transaction + pengaman stok.
  Klik 2x baris keranjang untuk menghapus item.
- **FormInventori:** CRUD Produk dan Supplier (2 tab), validasi input,
  supplier yang masih dipakai produk tidak bisa dihapus.
- **FormLaporan:** JOIN manual 3 database (produk terlaris + supplier + omzet),
  otomatis tampil saat form dibuka.

## Struktur

```
SistemTokoInventori/
├── SistemTokoInventori.sln
├── .gitignore
└── SistemTokoInventori/
    ├── Program.cs                    # Entry point
    ├── App.config
    ├── ConnectionManager.cs          # Path 3 DB + schema SQLite + auto-init + test koneksi
    ├── libs/                         # SQLite dibundel (tanpa NuGet)
    │   ├── System.Data.SQLite.dll
    │   ├── x64/SQLite.Interop.dll
    │   └── x86/SQLite.Interop.dll
    ├── FormMain.cs / .Designer.cs
    ├── FormKasir.cs / .Designer.cs
    ├── FormInventori.cs / .Designer.cs
    ├── FormLaporan.cs / .Designer.cs
    ├── Properties/
    └── SistemTokoInventori.csproj
```

## Database

| File (dibuat otomatis) | Tabel | Isi |
|---|---|---|
| `DB_Produk.db` | `Produk` | ID_Produk, Nama_Produk, Harga, Stok, ID_Supplier |
| `DB_Transaksi.db` | `Transaksi`, `Detail_Transaksi` | Header + detail penjualan |
| `DB_Supplier.db` | `Supplier` | ID_Supplier, Nama_Supplier, Kontak, Alamat |

## Catatan teknis (untuk presentasi)

- **Kenapa 3 database?** Mensimulasikan data yang terdistribusi; JOIN-nya
  dilakukan manual di C# (`FormLaporan.cs`) karena SQLite tidak bisa JOIN lintas file.
- **Auto-init:** `ConnectionManager.EnsureDatabases()` membuat 3 file `.db`
  dari schema yang tertanam di kode, hanya jika tabel utamanya belum ada
  (data lama tidak tertimpa).
- **Transaksi kasir:** stok dikurangi dulu dengan pengaman `Stok >= Qty`;
  kalau pencatatan transaksi gagal, stok dikembalikan (kompensasi lintas database).
- **Laporan:** hasil `SUM()` dibaca dengan `Convert.ToInt32/ToDecimal`
  (bukan `GetInt32/GetDecimal`) supaya tidak InvalidCastException.

## Troubleshooting

- **Masih CS0234/CS0246 SQLite** → kamu membuka project lama. Tutup VS,
  clone/ekstrak repo ini ke folder baru, buka `.sln` dari sana. Jangan campur.
- **Error SQLite.Interop.dll not found saat Run** → Clean + Build ulang;
  pastikan `x64\SQLite.Interop.dll` dan `x86\SQLite.Interop.dll` ada di `bin\Debug`.
- **Database Error di FormMain** → lihat isi MessageBox-nya, kirim teksnya.

## Presentasi

1. Jelaskan 3 database terpisah (Produk, Transaksi, Supplier).
2. Demo CRUD di FormInventori (tab Produk dan Supplier) — tunjukkan validasi input
   dan proteksi hapus supplier yang masih dipakai.
3. Demo transaksi di FormKasir (pilih produk → qty → tambah cart → proses);
   tunjukkan stok berkurang dan keranjang bisa dihapus (klik 2x).
4. Demo laporan di FormLaporan (join 3 database → produk terlaris + omzet).
5. Tunjukkan JOIN manual di `FormLaporan.cs` dan pengaman transaksi di `FormKasir.cs`.
