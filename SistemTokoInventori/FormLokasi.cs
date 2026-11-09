using System;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace SistemTokoInventori
{
    /// <summary>
    /// Modal tambah / ubah lokasi (gudang) — Fase 3. IdLokasi null =
    /// lokasi baru; terisi = ubah lokasi yang sudah ada. Kode harus unik
    /// (dicek pemanggil sebelum menyimpan). UI dibangun dari kode (tanpa
    /// Designer) seperti FormCatatPembayaran.
    /// </summary>
    public partial class FormLokasi : Form
    {
        // Disimpan di field supaya Guna2Elipse tidak di-GC.
        private Guna2Elipse formElipse;

        private Label lblKode;
        private Guna2TextBox txtKode;
        private Label lblNama;
        private Guna2TextBox txtNama;
        private Label lblAlamat;
        private Guna2TextBox txtAlamat;
        private Label lblWarning;
        private Guna2Button btnKonfirmasi;
        private Guna2Button btnBatal;

        // null = lokasi baru; terisi = ubah lokasi ini.
        public int? IdLokasi { get; set; }
        public string Kode { get; set; }
        public string Nama { get; set; }
        public string Alamat { get; set; }

        public FormLokasi()
        {
            InitializeComponent();
            TerapkanTema();
            this.Load += FormLokasi_Load;
        }

        private void FormLokasi_Load(object sender, EventArgs e)
        {
            this.Text = IdLokasi.HasValue ? "Ubah Lokasi" : "Tambah Lokasi";
            var header = Theme.BuildHeader(this.Text, "");
            this.Controls.Add(header);
            header.BringToFront();
            txtKode.Text = Kode ?? "";
            txtNama.Text = Nama ?? "";
            txtAlamat.Text = Alamat ?? "";
            PerbaruiValidasi();
        }

        private void InitializeComponent()
        {
            this.lblKode = new Label();
            this.txtKode = new Guna2TextBox();
            this.lblNama = new Label();
            this.txtNama = new Guna2TextBox();
            this.lblAlamat = new Label();
            this.txtAlamat = new Guna2TextBox();
            this.lblWarning = new Label();
            this.btnKonfirmasi = new Guna2Button();
            this.btnBatal = new Guna2Button();
            this.SuspendLayout();
            //
            // lblKode
            //
            this.lblKode.AutoSize = true;
            this.lblKode.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblKode.Location = new Point(20, 76);
            this.lblKode.Name = "lblKode";
            this.lblKode.Size = new Size(50, 19);
            this.lblKode.Text = "Kode:";
            //
            // txtKode
            //
            this.txtKode.Font = new Font("Segoe UI", 12F);
            this.txtKode.Location = new Point(20, 100);
            this.txtKode.Name = "txtKode";
            this.txtKode.PlaceholderText = "Contoh: G2";
            this.txtKode.Size = new Size(420, 42);
            this.txtKode.TextChanged += new EventHandler(this.txtKode_TextChanged);
            //
            // lblNama
            //
            this.lblNama.AutoSize = true;
            this.lblNama.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblNama.Location = new Point(20, 156);
            this.lblNama.Name = "lblNama";
            this.lblNama.Size = new Size(50, 19);
            this.lblNama.Text = "Nama:";
            //
            // txtNama
            //
            this.txtNama.Font = new Font("Segoe UI", 12F);
            this.txtNama.Location = new Point(20, 180);
            this.txtNama.Name = "txtNama";
            this.txtNama.PlaceholderText = "Contoh: GUDANG 2";
            this.txtNama.Size = new Size(420, 42);
            this.txtNama.TextChanged += new EventHandler(this.txtNama_TextChanged);
            //
            // lblAlamat
            //
            this.lblAlamat.AutoSize = true;
            this.lblAlamat.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblAlamat.Location = new Point(20, 236);
            this.lblAlamat.Name = "lblAlamat";
            this.lblAlamat.Size = new Size(60, 19);
            this.lblAlamat.Text = "Alamat:";
            //
            // txtAlamat
            //
            this.txtAlamat.Font = new Font("Segoe UI", 12F);
            this.txtAlamat.Location = new Point(20, 260);
            this.txtAlamat.Name = "txtAlamat";
            this.txtAlamat.PlaceholderText = "Alamat gudang (opsional)";
            this.txtAlamat.Size = new Size(420, 42);
            //
            // lblWarning
            //
            this.lblWarning.AutoSize = false;
            this.lblWarning.Font = new Font("Segoe UI", 10F);
            this.lblWarning.Location = new Point(20, 312);
            this.lblWarning.Name = "lblWarning";
            this.lblWarning.Size = new Size(420, 20);
            //
            // btnKonfirmasi
            //
            this.btnKonfirmasi.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.btnKonfirmasi.Location = new Point(20, 344);
            this.btnKonfirmasi.Name = "btnKonfirmasi";
            this.btnKonfirmasi.Size = new Size(200, 42);
            this.btnKonfirmasi.Text = "Simpan";
            this.btnKonfirmasi.Click += new EventHandler(this.btnKonfirmasi_Click);
            //
            // btnBatal
            //
            this.btnBatal.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.btnBatal.Location = new Point(240, 344);
            this.btnBatal.Name = "btnBatal";
            this.btnBatal.Size = new Size(200, 42);
            this.btnBatal.Text = "Batal";
            this.btnBatal.Click += new EventHandler(this.btnBatal_Click);
            //
            // FormLokasi
            //
            this.ClientSize = new Size(460, 408);
            this.Controls.Add(this.lblKode);
            this.Controls.Add(this.txtKode);
            this.Controls.Add(this.lblNama);
            this.Controls.Add(this.txtNama);
            this.Controls.Add(this.lblAlamat);
            this.Controls.Add(this.txtAlamat);
            this.Controls.Add(this.lblWarning);
            this.Controls.Add(this.btnKonfirmasi);
            this.Controls.Add(this.btnBatal);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormLokasi";
            this.StartPosition = FormStartPosition.CenterParent;
            this.ResumeLayout(false);
        }

        private void TerapkanTema()
        {
            Theme.StyleForm(this);
            formElipse = Theme.ApplyRoundedForm(this);
            lblKode.ForeColor = Theme.Text;
            lblNama.ForeColor = Theme.Text;
            lblAlamat.ForeColor = Theme.Text;
            lblWarning.ForeColor = Theme.Danger;
            Theme.StyleTextBox(txtKode);
            Theme.StyleTextBox(txtNama);
            Theme.StyleTextBox(txtAlamat);
            Theme.StyleSuccessButton(btnKonfirmasi);
            Theme.StyleGhostButton(btnBatal);
        }

        private void txtKode_TextChanged(object sender, EventArgs e)
        {
            PerbaruiValidasi();
        }

        private void txtNama_TextChanged(object sender, EventArgs e)
        {
            PerbaruiValidasi();
        }

        // Validasi live: Kode dan Nama wajib diisi.
        private void PerbaruiValidasi()
        {
            string pesan = "";
            if (string.IsNullOrWhiteSpace(txtKode.Text))
                pesan = "Kode lokasi wajib diisi.";
            else if (string.IsNullOrWhiteSpace(txtNama.Text))
                pesan = "Nama lokasi wajib diisi.";
            lblWarning.Text = pesan;
            btnKonfirmasi.Enabled = pesan.Length == 0;
        }

        private void btnKonfirmasi_Click(object sender, EventArgs e)
        {
            PerbaruiValidasi();
            if (!btnKonfirmasi.Enabled) return;
            Kode = txtKode.Text.Trim().ToUpperInvariant();
            Nama = txtNama.Text.Trim();
            Alamat = txtAlamat.Text.Trim();
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
