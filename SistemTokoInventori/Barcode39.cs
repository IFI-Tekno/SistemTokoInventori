using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace SistemTokoInventori
{
    /// <summary>
    /// Code 39 (tanpa library eksternal): tabel encoding narrow/wide
    /// ditulis sendiri, digambar dengan System.Drawing.
    ///
    /// Setiap karakter = 9 elemen (bar dan spasi bergantian, diawali bar),
    /// tepat 3 di antaranya wide. '*' adalah karakter start/stop dan
    /// tidak boleh muncul di dalam nilai. Karakter yang boleh di-encode:
    /// 0-9, A-Z, spasi, '-', '.', '$', '/', '+', '%'.
    /// Lebar narrow : wide = 1 : 3, antar-karakter dipisah satu spasi
    /// narrow. Nilai selalu di-encode dalam huruf besar.
    /// </summary>
    public static class Barcode39
    {
        // Pola per karakter: 'n' = narrow, 'w' = wide.
        // Indeks genap (0,2,4,6,8) = bar, indeks ganjil = spasi.
        public static readonly Dictionary<char, string> Patterns =
            new Dictionary<char, string>
        {
            { '0', "nnnwwnwnn" }, { '1', "wnnwnnnnw" }, { '2', "nnwwnnnnw" },
            { '3', "wnwwnnnnn" }, { '4', "nnnwwnnnw" }, { '5', "wnnwwnnnn" },
            { '6', "nnwwwnnnn" }, { '7', "nnnwnnwnw" }, { '8', "wnnwnnwnn" },
            { '9', "nnwwnnwnn" },
            { 'A', "wnnnnwnnw" }, { 'B', "nnwnnwnnw" }, { 'C', "wnwnnwnnn" },
            { 'D', "nnnnwwnnw" }, { 'E', "wnnnwwnnn" }, { 'F', "nnwnwwnnn" },
            { 'G', "nnnnnwwnw" }, { 'H', "wnnnnwwnn" }, { 'I', "nnwnnwwnn" },
            { 'J', "nnnnwwwnn" }, { 'K', "wnnnnnnww" }, { 'L', "nnwnnnnww" },
            { 'M', "wnwnnnnwn" }, { 'N', "nnnnwnnww" }, { 'O', "wnnnwnnwn" },
            { 'P', "nnwnwnnwn" }, { 'Q', "nnnnnnwww" }, { 'R', "wnnnnnwwn" },
            { 'S', "nnwnnnwwn" }, { 'T', "nnnnwnwwn" }, { 'U', "wwnnnnnnw" },
            { 'V', "nwwnnnnnw" }, { 'W', "wwwnnnnnn" }, { 'X', "nwnnwnnnw" },
            { 'Y', "wwnnwnnnn" }, { 'Z', "nwwnwnnnn" },
            { '-', "nwnnnnwnw" }, { '.', "wwnnnnwnn" }, { ' ', "nwwnnnwnn" },
            { '$', "nwnwnwnnn" }, { '/', "nwnwnnnwn" }, { '+', "nwnnnwnwn" },
            { '%', "nnnwnwnwn" },
            { '*', "nwnnwnwnn" },
        };

        /// <summary>
        /// Karakter yang boleh di-encode (di luar '*' start/stop).
        /// </summary>
        public const string AllowedChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ -. $/+%";

        /// <summary>
        /// Cek apakah nilai bisa di-encode. Kembalikan null kalau valid,
        /// atau pesan error yang menjelaskan karakter yang tidak didukung.
        /// </summary>
        public static string Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Nilai barcode kosong — isi Barcode atau Kode item dulu.";
            string atas = value.Trim().ToUpperInvariant();
            foreach (char c in atas)
            {
                if (c == '*')
                    return "Karakter '*' tidak boleh dipakai di nilai barcode " +
                        "(dipakai sebagai tanda awal/akhir Code 39).";
                if (!Patterns.ContainsKey(c))
                    return "Karakter '" + c + "' tidak bisa di-encode Code 39.\n" +
                        "Yang didukung: huruf A-Z, angka 0-9, spasi, dan - . $ / + %.";
            }
            return null;
        }

        /// <summary>
        /// Lebar total barcode (dalam pixel) untuk nilai + lebar narrow.
        /// Termasuk quiet zone 10x narrow di kiri dan kanan.
        /// </summary>
        public static int GetWidth(string value, int narrowWidth)
        {
            string full = "*" + value.Trim().ToUpperInvariant() + "*";
            int units = 20; // quiet zone kiri + kanan (10 narrow tiap sisi)
            for (int i = 0; i < full.Length; i++)
            {
                string pola = Patterns[full[i]];
                foreach (char c in pola)
                    units += (c == 'w') ? 3 : 1;
                if (i < full.Length - 1)
                    units += 1; // spasi antar-karakter (1 narrow)
            }
            return units * narrowWidth;
        }

        /// <summary>
        /// Gambar barcode Code 39 sebagai Bitmap (latar putih, bar hitam).
        /// </summary>
        public static Bitmap Render(string value, int narrowWidth, int barHeight)
        {
            if (narrowWidth < 1) narrowWidth = 1;
            if (barHeight < 1) barHeight = 1;
            string error = Validate(value);
            if (error != null)
                throw new ArgumentException(error, "value");

            string full = "*" + value.Trim().ToUpperInvariant() + "*";
            int lebar = GetWidth(value, narrowWidth);
            Bitmap bmp = new Bitmap(lebar, barHeight);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                int x = 10 * narrowWidth; // quiet zone kiri
                for (int i = 0; i < full.Length; i++)
                {
                    string pola = Patterns[full[i]];
                    for (int j = 0; j < pola.Length; j++)
                    {
                        int w = (pola[j] == 'w') ? 3 * narrowWidth : narrowWidth;
                        bool isBar = (j % 2 == 0); // posisi genap = bar
                        if (isBar)
                            g.FillRectangle(Brushes.Black, x, 0, w, barHeight);
                        x += w;
                    }
                    if (i < full.Length - 1)
                        x += narrowWidth; // spasi antar-karakter
                }
            }
            return bmp;
        }

        /// <summary>Teks yang dicetak di bawah barcode (nilai + tanda *).</summary>
        public static string DisplayText(string value)
        {
            return "*" + value.Trim().ToUpperInvariant() + "*";
        }
    }
}
