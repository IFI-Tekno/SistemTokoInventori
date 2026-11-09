using System;
using System.Globalization;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace SistemTokoInventori
{
    /// <summary>
    /// Dialog pembayaran (Tunai / Tempo). Dibuka dari KasirControl setelah
    /// keranjang terisi dan stok dicek, SEBELUM stok dikurangi / transaksi
    /// disimpan. Tunai: konfirmasi diblokir selama Bayar &lt; Total. Tempo:
    /// butuh pelanggan (AdaPelanggan), Bayar boleh 0 (full tempo) sampai
    /// Total (DP sebagian); sisanya jadi Piutang di KasirControl. Kalau
    /// dibatalkan, KasirControl berhenti tanpa mengubah apa pun.
    /// </summary>
    public partial class FormBayar : Form
    {
        // Disimpan di field supaya Guna2Elipse tidak di-GC.
        private Guna2Elipse formElipse;

        // Total tagihan — diisi KasirControl sebelum ShowDialog().
        public decimal Total { get; set; }

        // True kalau transaksi ini punya pelanggan (dipilih di Kasir).
        // Tempo hanya boleh kalau ini true.
        public bool AdaPelanggan { get; set; }

        // Diisi saat konfirmasi berhasil (hanya kalau DialogResult == OK).
        public decimal Bayar { get; private set; }
        public decimal Kembalian { get; private set; }
        // "Tunai" atau "Tempo".
        public string JenisBayar { get; private set; }
        // Berlaku untuk Tempo (default: hari ini + 14 hari).
        public DateTime JatuhTempo { get; private set; }

        public FormBayar()
        {
            InitializeComponent();
            JenisBayar = "Tunai";
            JatuhTempo = DateTime.Now.AddDays(14).Date;
            dtpJatuhTempo.Value = JatuhTempo;
            TerapkanTema();
            PerbaruiKembalian();
        }

        private void TerapkanTema()
        {
            Theme.StyleForm(this);
            formElipse = Theme.ApplyRoundedForm(this);
            Theme.StyleTextBox(txtBayar);
            Theme.StyleDatePicker(dtpJatuhTempo);
            Theme.StylePrimaryButton(btnKonfirmasi);
            Theme.StyleGhostButton(btnBatal);
            lblTotal.ForeColor = Theme.Text;
            lblBayar.ForeColor = Theme.Text;
            rdoTunai.ForeColor = Theme.Text;
            rdoTempo.ForeColor = Theme.Text;
            lblJatuhTempo.ForeColor = Theme.Text;
            lblKembalian.ForeColor = Theme.SuccessDark;
            lblWarning.ForeColor = Theme.Danger;
            var header = Theme.BuildHeader("Pembayaran", "Tunai / Tempo — masukkan uang diterima");
            this.Controls.Add(header);
        }

        // Sama seperti MasterDataControl.TryParseDecimal: CurrentCulture (id-ID)
        // dulu ("50.000" = 50000), baru InvariantCulture sebagai fallback.
        private static bool TryParseDecimal(string teks, out decimal nilai)
        {
            teks = (teks ?? "").Trim();
            if (decimal.TryParse(teks, NumberStyles.Number, CultureInfo.CurrentCulture, out nilai))
                return true;
            return decimal.TryParse(teks, NumberStyles.Number, CultureInfo.InvariantCulture, out nilai);
        }

        // Hitung kembalian / sisa piutang live setiap kali input berubah.
        // Tunai: tombol konfirmasi hanya aktif kalau uang valid DAN cukup.
        // Tempo: butuh pelanggan; Bayar 0..Total (0 = full tempo, di antara
        // = DP sebagian); tidak ada kembalian, yang tampil sisa piutang.
        private void PerbaruiKembalian()
        {
            lblTotal.Text = "Total: Rp " + Total.ToString("N0");

            if (rdoTempo.Checked)
            {
                if (!AdaPelanggan)
                {
                    lblKembalian.Text = "Sisa piutang: Rp " + Total.ToString("N0");
                    lblWarning.Text = "Pembayaran tempo butuh pelanggan! Pilih pelanggan di Kasir.";
                    lblWarning.Visible = true;
                    btnKonfirmasi.Enabled = false;
                    return;
                }
                decimal bayarTempo = 0;
                if (!string.IsNullOrWhiteSpace(txtBayar.Text))
                {
                    if (!TryParseDecimal(txtBayar.Text, out bayarTempo))
                    {
                        lblKembalian.Text = "Sisa piutang: Rp " + Total.ToString("N0");
                        lblWarning.Text = "Masukkan jumlah uang yang valid.";
                        lblWarning.Visible = true;
                        btnKonfirmasi.Enabled = false;
                        return;
                    }
                }
                if (bayarTempo < 0 || bayarTempo > Total)
                {
                    lblKembalian.Text = "Sisa piutang: Rp " + Total.ToString("N0");
                    lblWarning.Text = "Bayar tempo antara Rp 0 sampai Rp " + Total.ToString("N0") + ".";
                    lblWarning.Visible = true;
                    btnKonfirmasi.Enabled = false;
                    return;
                }
                lblKembalian.Text = "Sisa piutang: Rp " + (Total - bayarTempo).ToString("N0");
                lblWarning.Visible = false;
                btnKonfirmasi.Enabled = true;
                return;
            }

            if (!TryParseDecimal(txtBayar.Text, out decimal bayar))
            {
                lblKembalian.Text = "Kembalian: Rp 0";
                lblWarning.Text = "Masukkan jumlah uang yang valid.";
                lblWarning.Visible = true;
                btnKonfirmasi.Enabled = false;
                return;
            }

            if (bayar < Total)
            {
                lblKembalian.Text = "Kembalian: Rp 0";
                lblWarning.Text = "Uang kurang Rp " + (Total - bayar).ToString("N0") + "!";
                lblWarning.Visible = true;
                btnKonfirmasi.Enabled = false;
                return;
            }

            lblKembalian.Text = "Kembalian: Rp " + (bayar - Total).ToString("N0");
            lblWarning.Visible = false;
            btnKonfirmasi.Enabled = true;
        }

        private void txtBayar_TextChanged(object sender, EventArgs e)
        {
            PerbaruiKembalian();
        }

        private void rdoJenisBayar_CheckedChanged(object sender, EventArgs e)
        {
            bool tempo = rdoTempo.Checked;
            lblJatuhTempo.Visible = tempo;
            dtpJatuhTempo.Visible = tempo;
            lblBayar.Text = tempo
                ? "DP / Bayar di Muka (Rp, boleh 0)"
                : "Uang Diterima (Rp)";
            PerbaruiKembalian();
        }

        private void btnKonfirmasi_Click(object sender, EventArgs e)
        {
            if (rdoTempo.Checked)
            {
                if (!AdaPelanggan)
                {
                    MessageBox.Show("Pembayaran tempo butuh pelanggan! Pilih pelanggan di Kasir.");
                    return;
                }
                decimal bayarTempo = 0;
                if (!string.IsNullOrWhiteSpace(txtBayar.Text))
                {
                    if (!TryParseDecimal(txtBayar.Text, out bayarTempo)
                        || bayarTempo < 0 || bayarTempo > Total)
                    {
                        MessageBox.Show("Bayar tempo harus antara Rp 0 sampai Rp " +
                            Total.ToString("N0") + ".");
                        return;
                    }
                }
                JenisBayar = "Tempo";
                JatuhTempo = dtpJatuhTempo.Value.Date;
                Bayar = bayarTempo;
                Kembalian = 0;
                this.DialogResult = DialogResult.OK;
                this.Close();
                return;
            }

            // Tunai — PerbaruiKembalian sudah menjamin bayar valid dan
            // >= Total, tapi parse ulang di sini sebagai pengaman terakhir.
            if (!TryParseDecimal(txtBayar.Text, out decimal bayar) || bayar < Total)
            {
                MessageBox.Show("Uang diterima kurang dari total!");
                return;
            }
            JenisBayar = "Tunai";
            JatuhTempo = dtpJatuhTempo.Value.Date;
            Bayar = bayar;
            Kembalian = bayar - Total;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnBatal_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
