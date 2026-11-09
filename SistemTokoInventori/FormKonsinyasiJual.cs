using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace SistemTokoInventori
{
    /// <summary>
    /// Modal generik untuk konsinyasi (Fase 3) — dipakai 3 kali:
    /// 1. "Catat Penjualan Titipan" (Konsinyasi Masuk): qty + harga jual.
    /// 2. "Catat Penjualan Agen" (Konsinyasi Keluar): qty + harga jual.
    /// 3. "Terima Kembali" (Konsinyasi Keluar): hanya qty
    ///    (TampilkanHarga = false).
    /// Qty divalidasi live: harus &gt; 0 dan &lt;= QtyMaks (sisa titipan).
    /// UI dibangun dari kode (tanpa Designer) seperti FormCatatPembayaran.
    /// </summary>
    public partial class FormKonsinyasiJual : Form
    {
        // Disimpan di field supaya Guna2Elipse tidak di-GC.
        private Guna2Elipse formElipse;

        private Label lblInfo;
        private Label lblSisa;
        private Label lblQty;
        private Guna2NumericUpDown numQty;
        private Label lblHarga;
        private Guna2TextBox txtHarga;
        private Label lblWarning;
        private Guna2Button btnKonfirmasi;
        private Guna2Button btnBatal;

        // Diisi pemanggil sebelum ShowDialog().
        public string Judul { get; set; }
        public string NamaItem { get; set; }
        // Sisa titipan yang masih bisa dijual / dikembalikan.
        public decimal QtyMaks { get; set; }
        public decimal HargaDefault { get; set; }
        // False untuk "Terima Kembali" (harga tidak relevan).
        public bool TampilkanHarga { get; set; }

        // Diisi saat konfirmasi berhasil (DialogResult == OK).
        public decimal Qty { get; private set; }
        public decimal HargaJual { get; private set; }

        public FormKonsinyasiJual()
        {
            TampilkanHarga = true;
            InitializeComponent();
            TerapkanTema();
            this.Load += FormKonsinyasiJual_Load;
        }

        private void FormKonsinyasiJual_Load(object sender, EventArgs e)
        {
            // Header dibangun di Load supaya judul dari pemanggil (diisi
            // setelah konstruktor) ikut tampil.
            this.Text = string.IsNullOrEmpty(Judul) ? "Konsinyasi" : Judul;
            var header = Theme.BuildHeader(this.Text, "");
            this.Controls.Add(header);
            header.BringToFront();
            lblInfo.Text = NamaItem ?? "";
            lblSisa.Text = "Sisa titipan: " + QtyMaks.ToString("N0");
            numQty.Maximum = QtyMaks > 0 ? QtyMaks : 1;
            numQty.Value = QtyMaks > 0 ? 1 : 0;
            txtHarga.Text = HargaDefault.ToString("N0");
            lblHarga.Visible = TampilkanHarga;
            txtHarga.Visible = TampilkanHarga;
            PerbaruiValidasi();
        }

        private void InitializeComponent()
        {
            this.lblInfo = new Label();
            this.lblSisa = new Label();
            this.lblQty = new Label();
            this.numQty = new Guna2NumericUpDown();
            this.lblHarga = new Label();
            this.txtHarga = new Guna2TextBox();
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
            this.lblSisa.Text = "Sisa titipan: 0";
            //
            // lblQty
            //
            this.lblQty.AutoSize = true;
            this.lblQty.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblQty.Location = new Point(20, 144);
            this.lblQty.Name = "lblQty";
            this.lblQty.Size = new Size(40, 19);
            this.lblQty.Text = "Qty:";
            //
            // numQty
            //
            this.numQty.Font = new Font("Segoe UI", 12F);
            this.numQty.Location = new Point(20, 168);
            this.numQty.Name = "numQty";
            this.numQty.Size = new Size(420, 42);
            this.numQty.Minimum = 0;
            this.numQty.Maximum = 99999;
            this.numQty.DecimalPlaces = 2;
            this.numQty.ValueChanged += new EventHandler(this.numQty_ValueChanged);
            //
            // lblHarga
            //
            this.lblHarga.AutoSize = true;
            this.lblHarga.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblHarga.Location = new Point(20, 224);
            this.lblHarga.Name = "lblHarga";
            this.lblHarga.Size = new Size(110, 19);
            this.lblHarga.Text = "Harga jual (Rp):";
            //
            // txtHarga
            //
            this.txtHarga.Font = new Font("Segoe UI", 12F);
            this.txtHarga.Location = new Point(20, 248);
            this.txtHarga.Name = "txtHarga";
            this.txtHarga.PlaceholderText = "Contoh: 50000";
            this.txtHarga.Size = new Size(420, 42);
            this.txtHarga.TextChanged += new EventHandler(this.txtHarga_TextChanged);
            //
            // lblWarning
            //
            this.lblWarning.AutoSize = false;
            this.lblWarning.Font = new Font("Segoe UI", 10F);
            this.lblWarning.Location = new Point(20, 300);
            this.lblWarning.Name = "lblWarning";
            this.lblWarning.Size = new Size(420, 20);
            //
            // btnKonfirmasi
            //
            this.btnKonfirmasi.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.btnKonfirmasi.Location = new Point(20, 336);
            this.btnKonfirmasi.Name = "btnKonfirmasi";
            this.btnKonfirmasi.Size = new Size(200, 42);
            this.btnKonfirmasi.Text = "Konfirmasi";
            this.btnKonfirmasi.Click += new EventHandler(this.btnKonfirmasi_Click);
            //
            // btnBatal
            //
            this.btnBatal.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.btnBatal.Location = new Point(240, 336);
            this.btnBatal.Name = "btnBatal";
            this.btnBatal.Size = new Size(200, 42);
            this.btnBatal.Text = "Batal";
            this.btnBatal.Click += new EventHandler(this.btnBatal_Click);
            //
            // FormKonsinyasiJual
            //
            this.ClientSize = new Size(460, 400);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.lblSisa);
            this.Controls.Add(this.lblQty);
            this.Controls.Add(this.numQty);
            this.Controls.Add(this.lblHarga);
            this.Controls.Add(this.txtHarga);
            this.Controls.Add(this.lblWarning);
            this.Controls.Add(this.btnKonfirmasi);
            this.Controls.Add(this.btnBatal);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormKonsinyasiJual";
            this.StartPosition = FormStartPosition.CenterParent;
            this.ResumeLayout(false);
        }

        private void TerapkanTema()
        {
            Theme.StyleForm(this);
            formElipse = Theme.ApplyRoundedForm(this);
            lblInfo.ForeColor = Theme.Text;
            lblSisa.ForeColor = Theme.Text;
            lblQty.ForeColor = Theme.Text;
            lblHarga.ForeColor = Theme.Text;
            lblWarning.ForeColor = Theme.Danger;
            Theme.StyleNumericUpDown(numQty);
            Theme.StyleTextBox(txtHarga);
            Theme.StyleSuccessButton(btnKonfirmasi);
            Theme.StyleGhostButton(btnBatal);
        }

        private static bool TryParseDecimal(string teks, out decimal nilai)
        {
            teks = (teks ?? "").Trim();
            if (decimal.TryParse(teks, NumberStyles.Number, CultureInfo.CurrentCulture, out nilai))
                return true;
            return decimal.TryParse(teks, NumberStyles.Number, CultureInfo.InvariantCulture, out nilai);
        }

        private void numQty_ValueChanged(object sender, EventArgs e)
        {
            PerbaruiValidasi();
        }

        private void txtHarga_TextChanged(object sender, EventArgs e)
        {
            PerbaruiValidasi();
        }

        // Validasi live: qty harus 0 < qty <= QtyMaks; harga (kalau tampil)
        // harus angka > 0. Tombol konfirmasi mati selama tidak valid.
        private void PerbaruiValidasi()
        {
            string pesan = "";
            if (numQty.Value <= 0)
                pesan = "Qty harus lebih dari 0.";
            else if (numQty.Value > QtyMaks)
                pesan = "Qty melebihi sisa titipan (" + QtyMaks.ToString("N0") + ").";
            else if (TampilkanHarga
                && (!TryParseDecimal(txtHarga.Text, out decimal harga) || harga <= 0))
                pesan = "Harga jual harus angka lebih dari 0.";
            lblWarning.Text = pesan;
            btnKonfirmasi.Enabled = pesan.Length == 0;
        }

        private void btnKonfirmasi_Click(object sender, EventArgs e)
        {
            PerbaruiValidasi();
            if (!btnKonfirmasi.Enabled) return;
            Qty = numQty.Value;
            HargaJual = 0;
            if (TampilkanHarga && TryParseDecimal(txtHarga.Text, out decimal harga))
                HargaJual = harga;
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
