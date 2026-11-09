using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace SistemTokoInventori
{
    /// <summary>
    /// Modal "Catat Pembayaran" generik (Fase 2) — dipakai tab Hutang di
    /// PembelianControl dan tab Piutang di PenjualanControl. Judul dan
    /// konteks (mis. supplier / nomor faktur) diisi pemanggil sebelum
    /// ShowDialog(). Jumlah divalidasi live: harus &gt; 0 dan &lt;= Sisa.
    /// UI dibangun dari kode (tanpa Designer) seperti MasterDataControl.
    /// </summary>
    public partial class FormCatatPembayaran : Form
    {
        // Disimpan di field supaya Guna2Elipse tidak di-GC.
        private Guna2Elipse formElipse;

        private Label lblInfo;
        private Label lblSisa;
        private Label lblJumlah;
        private Guna2TextBox txtJumlah;
        private Label lblTanggal;
        private Guna2DateTimePicker dtpTanggal;
        private Label lblKeterangan;
        private Guna2TextBox txtKeterangan;
        private Label lblWarning;
        private Guna2Button btnKonfirmasi;
        private Guna2Button btnBatal;

        // Judul dialog ("Catat Pembayaran Hutang" / "Catat Pembayaran
        // Piutang") + konteks ("PT Indofood — PO-2026-0001" /
        // "Budi — INV-12"). Diisi pemanggil sebelum ShowDialog().
        public string Judul { get; set; }
        public string Konteks { get; set; }

        // Sisa tagihan — batas atas Jumlah.
        public decimal Sisa { get; set; }

        // Diisi saat konfirmasi berhasil (DialogResult == OK).
        public decimal Jumlah { get; private set; }
        public DateTime Tanggal { get; private set; }
        public string Keterangan { get; private set; }

        public FormCatatPembayaran()
        {
            InitializeComponent();
            TerapkanTema();
            dtpTanggal.Value = DateTime.Now.Date;
            this.Load += FormCatatPembayaran_Load;
        }

        private void FormCatatPembayaran_Load(object sender, EventArgs e)
        {
            // Header dibangun di Load supaya judul + konteks dari pemanggil
            // (diisi setelah konstruktor) ikut tampil.
            this.Text = string.IsNullOrEmpty(Judul) ? "Catat Pembayaran" : Judul;
            var header = Theme.BuildHeader(this.Text, "");
            this.Controls.Add(header);
            header.BringToFront();
            lblInfo.Text = Konteks ?? "";
            lblSisa.Text = "Sisa: Rp " + Sisa.ToString("N0");
            PerbaruiValidasi();
        }

        private void InitializeComponent()
        {
            this.lblInfo = new Label();
            this.lblSisa = new Label();
            this.lblJumlah = new Label();
            this.txtJumlah = new Guna2TextBox();
            this.lblTanggal = new Label();
            this.dtpTanggal = new Guna2DateTimePicker();
            this.lblKeterangan = new Label();
            this.txtKeterangan = new Guna2TextBox();
            this.lblWarning = new Label();
            this.btnKonfirmasi = new Guna2Button();
            this.btnBatal = new Guna2Button();
            this.SuspendLayout();
            //
            // lblInfo
            //
            this.lblInfo.AutoSize = false;
            this.lblInfo.Font = new Font("Segoe UI", 10F);
            this.lblInfo.Location = new Point(20, 76);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new Size(420, 20);
            //
            // lblSisa
            //
            this.lblSisa.AutoSize = false;
            this.lblSisa.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            this.lblSisa.Location = new Point(20, 100);
            this.lblSisa.Name = "lblSisa";
            this.lblSisa.Size = new Size(420, 36);
            this.lblSisa.Text = "Sisa: Rp 0";
            //
            // lblJumlah
            //
            this.lblJumlah.AutoSize = true;
            this.lblJumlah.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblJumlah.Location = new Point(20, 144);
            this.lblJumlah.Name = "lblJumlah";
            this.lblJumlah.Size = new Size(140, 19);
            this.lblJumlah.Text = "Jumlah bayar (Rp):";
            //
            // txtJumlah
            //
            this.txtJumlah.Font = new Font("Segoe UI", 12F);
            this.txtJumlah.Location = new Point(20, 168);
            this.txtJumlah.Name = "txtJumlah";
            this.txtJumlah.PlaceholderText = "Contoh: 50000";
            this.txtJumlah.Size = new Size(420, 42);
            this.txtJumlah.TextChanged += new EventHandler(this.txtJumlah_TextChanged);
            //
            // lblTanggal
            //
            this.lblTanggal.AutoSize = true;
            this.lblTanggal.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblTanggal.Location = new Point(20, 222);
            this.lblTanggal.Name = "lblTanggal";
            this.lblTanggal.Size = new Size(65, 19);
            this.lblTanggal.Text = "Tanggal:";
            //
            // dtpTanggal
            //
            this.dtpTanggal.Font = new Font("Segoe UI", 10F);
            this.dtpTanggal.Location = new Point(20, 246);
            this.dtpTanggal.Name = "dtpTanggal";
            this.dtpTanggal.Size = new Size(200, 30);
            //
            // lblKeterangan
            //
            this.lblKeterangan.AutoSize = true;
            this.lblKeterangan.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblKeterangan.Location = new Point(20, 288);
            this.lblKeterangan.Name = "lblKeterangan";
            this.lblKeterangan.Size = new Size(90, 19);
            this.lblKeterangan.Text = "Keterangan:";
            //
            // txtKeterangan
            //
            this.txtKeterangan.Font = new Font("Segoe UI", 10F);
            this.txtKeterangan.Location = new Point(20, 312);
            this.txtKeterangan.Name = "txtKeterangan";
            this.txtKeterangan.PlaceholderText = "Opsional";
            this.txtKeterangan.Size = new Size(420, 36);
            //
            // lblWarning
            //
            this.lblWarning.AutoSize = false;
            this.lblWarning.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblWarning.Location = new Point(20, 356);
            this.lblWarning.Name = "lblWarning";
            this.lblWarning.Size = new Size(420, 24);
            this.lblWarning.Visible = false;
            //
            // btnKonfirmasi
            //
            this.btnKonfirmasi.Enabled = false;
            this.btnKonfirmasi.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            this.btnKonfirmasi.Location = new Point(20, 392);
            this.btnKonfirmasi.Name = "btnKonfirmasi";
            this.btnKonfirmasi.Size = new Size(200, 46);
            this.btnKonfirmasi.Text = "Simpan Pembayaran";
            this.btnKonfirmasi.Click += new EventHandler(this.btnKonfirmasi_Click);
            //
            // btnBatal
            //
            this.btnBatal.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            this.btnBatal.Location = new Point(240, 392);
            this.btnBatal.Name = "btnBatal";
            this.btnBatal.Size = new Size(200, 46);
            this.btnBatal.Text = "Batal";
            this.btnBatal.Click += new EventHandler(this.btnBatal_Click);
            //
            // FormCatatPembayaran
            //
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(460, 460);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.lblSisa);
            this.Controls.Add(this.lblJumlah);
            this.Controls.Add(this.txtJumlah);
            this.Controls.Add(this.lblTanggal);
            this.Controls.Add(this.dtpTanggal);
            this.Controls.Add(this.lblKeterangan);
            this.Controls.Add(this.txtKeterangan);
            this.Controls.Add(this.lblWarning);
            this.Controls.Add(this.btnKonfirmasi);
            this.Controls.Add(this.btnBatal);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormCatatPembayaran";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Catat Pembayaran";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void TerapkanTema()
        {
            Theme.StyleForm(this);
            formElipse = Theme.ApplyRoundedForm(this);
            Theme.StyleTextBox(txtJumlah);
            Theme.StyleTextBox(txtKeterangan);
            Theme.StyleDatePicker(dtpTanggal);
            Theme.StylePrimaryButton(btnKonfirmasi);
            Theme.StyleGhostButton(btnBatal);
            lblInfo.ForeColor = Theme.Muted;
            lblSisa.ForeColor = Theme.Text;
            lblJumlah.ForeColor = Theme.Text;
            lblTanggal.ForeColor = Theme.Text;
            lblKeterangan.ForeColor = Theme.Text;
            lblWarning.ForeColor = Theme.Danger;
        }

        // Sama seperti FormBayar.TryParseDecimal: CurrentCulture (id-ID)
        // dulu ("50.000" = 50000), baru InvariantCulture sebagai fallback.
        private static bool TryParseDecimal(string teks, out decimal nilai)
        {
            teks = (teks ?? "").Trim();
            if (decimal.TryParse(teks, NumberStyles.Number, CultureInfo.CurrentCulture, out nilai))
                return true;
            return decimal.TryParse(teks, NumberStyles.Number, CultureInfo.InvariantCulture, out nilai);
        }

        private void PerbaruiValidasi()
        {
            if (!TryParseDecimal(txtJumlah.Text, out decimal jumlah))
            {
                lblWarning.Text = "Masukkan jumlah yang valid.";
                lblWarning.Visible = true;
                btnKonfirmasi.Enabled = false;
                return;
            }
            if (jumlah <= 0)
            {
                lblWarning.Text = "Jumlah harus lebih dari 0.";
                lblWarning.Visible = true;
                btnKonfirmasi.Enabled = false;
                return;
            }
            if (jumlah > Sisa)
            {
                lblWarning.Text = "Jumlah tidak boleh melebihi sisa Rp " + Sisa.ToString("N0") + ".";
                lblWarning.Visible = true;
                btnKonfirmasi.Enabled = false;
                return;
            }
            lblWarning.Visible = false;
            btnKonfirmasi.Enabled = true;
        }

        private void txtJumlah_TextChanged(object sender, EventArgs e)
        {
            PerbaruiValidasi();
        }

        private void btnKonfirmasi_Click(object sender, EventArgs e)
        {
            if (!TryParseDecimal(txtJumlah.Text, out decimal jumlah)
                || jumlah <= 0 || jumlah > Sisa)
            {
                MessageBox.Show("Jumlah bayar harus lebih dari 0 dan tidak melebihi sisa Rp " +
                    Sisa.ToString("N0") + ".");
                return;
            }
            Jumlah = jumlah;
            Tanggal = dtpTanggal.Value.Date;
            Keterangan = txtKeterangan.Text.Trim();
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
