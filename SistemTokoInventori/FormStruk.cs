using System;
using System.Data;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace SistemTokoInventori
{
    /// <summary>
    /// Struk belanja (hanya tampil, tanpa cetak). Dibuka dari KasirControl
    /// setelah transaksi tersimpan. Isinya: nama toko, nomor + tanggal
    /// transaksi, satu baris per item keranjang, Subtotal, Diskon, Poin
    /// dipakai, Tukar tambah, Deposit dipakai, Total, Bayar, lalu Kembalian
    /// (Tunai) atau Sisa piutang + Jatuh tempo (Tempo). Kolom keranjang
    /// mengikuti skema v2 (Nama, HargaJual).
    /// </summary>
    public partial class FormStruk : Form
    {
        // Disimpan di field supaya Guna2Elipse tidak di-GC.
        private Guna2Elipse formElipse;

        // Konstruktor lama (tanpa rincian diskon/poin/tukar/deposit):
        // didelegasikan ke konstruktor baru dengan rincian 0 dan Tunai.
        public FormStruk(long idTransaksi, DateTime tanggal, DataTable items,
            decimal total, decimal bayar, decimal kembalian)
            : this(idTransaksi, tanggal, items, total, 0, 0, 0, 0,
                total, bayar, kembalian, false, 0, DateTime.Now)
        {
        }

        public FormStruk(long idTransaksi, DateTime tanggal, DataTable items,
            decimal subtotal, decimal diskon, decimal nilaiPoin, decimal nilaiTukar,
            decimal nilaiDeposit, decimal total, decimal bayar, decimal kembalian,
            bool isTempo, decimal sisaPiutang, DateTime jatuhTempo)
        {
            InitializeComponent();
            TerapkanTema(idTransaksi);
            txtStruk.Text = BangunStruk(idTransaksi, tanggal, items,
                subtotal, diskon, nilaiPoin, nilaiTukar, nilaiDeposit,
                total, bayar, kembalian, isTempo, sisaPiutang, jatuhTempo);
        }

        private void TerapkanTema(long idTransaksi)
        {
            Theme.StyleForm(this);
            formElipse = Theme.ApplyRoundedForm(this);
            Theme.StyleTextBox(txtStruk);
            Theme.StylePrimaryButton(btnTutup);
            var header = Theme.BuildHeader("Struk Pembayaran",
                "No. Transaksi " + idTransaksi);
            this.Controls.Add(header);
        }

        private static string BangunStruk(long idTransaksi, DateTime tanggal,
            DataTable items, decimal subtotal, decimal diskon, decimal nilaiPoin,
            decimal nilaiTukar, decimal nilaiDeposit, decimal total, decimal bayar,
            decimal kembalian, bool isTempo, decimal sisaPiutang, DateTime jatuhTempo)
        {
            CultureInfo id = new CultureInfo("id-ID");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("SISTEM TOKO INVENTORI");
            sb.AppendLine("======================================");
            sb.AppendLine("No. Transaksi : " + idTransaksi);
            sb.AppendLine("Tanggal       : " + tanggal.ToString("dd MMM yyyy HH:mm", id));
            sb.AppendLine("--------------------------------------");
            foreach (DataRow row in items.Rows)
            {
                string nama = Convert.ToString(row["Nama"]);
                int qty = Convert.ToInt32(row["Qty"]);
                decimal harga = Convert.ToDecimal(row["HargaJual"]);
                decimal subtotalBaris = Convert.ToDecimal(row["Subtotal"]);
                // Kalau ada DiskonItem (keranjang dari Sales Order), harga
                // yang tampil adalah harga efektif supaya
                // qty x harga = subtotal tetap konsisten.
                if (items.Columns.Contains("DiskonItem"))
                    harga = harga - Convert.ToDecimal(row["DiskonItem"]);
                sb.AppendLine(nama);
                sb.AppendLine("  " + qty + " x " + harga.ToString("N0", id) +
                    " = " + subtotalBaris.ToString("N0", id));
            }
            sb.AppendLine("--------------------------------------");
            sb.AppendLine("Subtotal      : Rp " + subtotal.ToString("N0", id));
            if (diskon > 0)
                sb.AppendLine("Diskon        : -Rp " + diskon.ToString("N0", id));
            if (nilaiPoin > 0)
                sb.AppendLine("Poin dipakai   : -Rp " + nilaiPoin.ToString("N0", id));
            if (nilaiTukar > 0)
                sb.AppendLine("Tukar tambah  : -Rp " + nilaiTukar.ToString("N0", id));
            if (nilaiDeposit > 0)
                sb.AppendLine("Deposit dipakai: -Rp " + nilaiDeposit.ToString("N0", id));
            sb.AppendLine("Total         : Rp " + total.ToString("N0", id));
            sb.AppendLine("Bayar         : Rp " + bayar.ToString("N0", id));
            if (isTempo)
            {
                sb.AppendLine("Sisa piutang  : Rp " + sisaPiutang.ToString("N0", id));
                sb.AppendLine("Jatuh tempo   : " + jatuhTempo.ToString("dd MMM yyyy", id));
            }
            else
            {
                sb.AppendLine("Kembalian     : Rp " + kembalian.ToString("N0", id));
            }
            sb.AppendLine("======================================");
            sb.AppendLine("Terima kasih sudah berbelanja!");
            return sb.ToString();
        }

        private void btnTutup_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
