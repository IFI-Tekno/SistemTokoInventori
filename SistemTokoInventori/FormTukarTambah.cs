using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace SistemTokoInventori
{
    /// <summary>
    /// Modal tukar tambah (Fase 2). Dibuka dari KasirControl. Isinya:
    /// deskripsi barang yang ditukar, item masuk (opsional — kalau dipilih
    /// item yang sudah ada dan melacak stok, stoknya naik saat transaksi
    /// selesai), qty masuk, dan nilai tukar. Nilai tidak boleh melebihi
    /// MaxNilai (total setelah diskon &amp; poin, diisi KasirControl).
    /// UI dibangun dari kode (tanpa Designer) seperti MasterDataControl.
    /// </summary>
    public partial class FormTukarTambah : Form
    {
        // Disimpan di field supaya Guna2Elipse tidak di-GC.
        private Guna2Elipse formElipse;

        private Label lblDeskripsi;
        private Guna2TextBox txtDeskripsi;
        private Label lblItemMasuk;
        private Guna2ComboBox cmbItemMasuk;
        private Label lblQtyMasuk;
        private Guna2NumericUpDown numQtyMasuk;
        private Label lblNilai;
        private Guna2TextBox txtNilai;
        private Label lblWarning;
        private Guna2Button btnKonfirmasi;
        private Guna2Button btnHapus;
        private Guna2Button btnBatal;

        // Batas nilai tukar (total setelah diskon & poin) — diisi
        // KasirControl sebelum ShowDialog().
        public decimal MaxNilai { get; set; }

        // Nilai awal (untuk mengedit tukar tambah yang sudah ada di
        // keranjang) — diisi KasirControl sebelum ShowDialog(), lalu
        // diterapkan ke kontrol saat form dimuat (event Load).
        public string Deskripsi { get; set; }
        public int? IdItemMasuk { get; set; }
        public decimal QtyMasuk { get; set; }
        public decimal NilaiTukar { get; set; }

        // Diisi saat konfirmasi / hapus berhasil (DialogResult == OK).
        // True kalau pengguna menekan "Hapus Tukar Tambah".
        public bool HapusDipilih { get; private set; }

        public FormTukarTambah()
        {
            InitializeComponent();
            TerapkanTema();
            LoadItemMasuk();
            this.Load += FormTukarTambah_Load;
        }

        private void FormTukarTambah_Load(object sender, EventArgs e)
        {
            txtDeskripsi.Text = Deskripsi ?? "";
            if (IdItemMasuk.HasValue)
            {
                foreach (DataRowView baris in cmbItemMasuk.Items)
                {
                    if (Convert.ToInt32(baris["ID_Item"]) == IdItemMasuk.Value)
                    {
                        cmbItemMasuk.SelectedValue = IdItemMasuk.Value;
                        break;
                    }
                }
            }
            numQtyMasuk.Value = QtyMasuk > 0 ? QtyMasuk : 1;
            txtNilai.Text = NilaiTukar > 0 ? NilaiTukar.ToString("N0") : "";
            btnHapus.Visible = NilaiTukar > 0;
        }

        private void InitializeComponent()
        {
            this.lblDeskripsi = new Label();
            this.txtDeskripsi = new Guna2TextBox();
            this.lblItemMasuk = new Label();
            this.cmbItemMasuk = new Guna2ComboBox();
            this.lblQtyMasuk = new Label();
            this.numQtyMasuk = new Guna2NumericUpDown();
            this.lblNilai = new Label();
            this.txtNilai = new Guna2TextBox();
            this.lblWarning = new Label();
            this.btnKonfirmasi = new Guna2Button();
            this.btnHapus = new Guna2Button();
            this.btnBatal = new Guna2Button();
            this.SuspendLayout();
            //
            // lblDeskripsi
            //
            this.lblDeskripsi.AutoSize = true;
            this.lblDeskripsi.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblDeskripsi.Location = new Point(20, 84);
            this.lblDeskripsi.Name = "lblDeskripsi";
            this.lblDeskripsi.Size = new Size(220, 19);
            this.lblDeskripsi.Text = "Deskripsi barang yang ditukar:";
            //
            // txtDeskripsi
            //
            this.txtDeskripsi.Font = new Font("Segoe UI", 10F);
            this.txtDeskripsi.Location = new Point(20, 108);
            this.txtDeskripsi.Name = "txtDeskripsi";
            this.txtDeskripsi.PlaceholderText = "Contoh: HP lama, layar retak";
            this.txtDeskripsi.Size = new Size(420, 36);
            //
            // lblItemMasuk
            //
            this.lblItemMasuk.AutoSize = true;
            this.lblItemMasuk.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblItemMasuk.Location = new Point(20, 156);
            this.lblItemMasuk.Name = "lblItemMasuk";
            this.lblItemMasuk.Size = new Size(200, 19);
            this.lblItemMasuk.Text = "Item masuk (opsional):";
            //
            // cmbItemMasuk
            //
            this.cmbItemMasuk.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbItemMasuk.Font = new Font("Segoe UI", 10F);
            this.cmbItemMasuk.Location = new Point(20, 180);
            this.cmbItemMasuk.Name = "cmbItemMasuk";
            this.cmbItemMasuk.Size = new Size(300, 36);
            //
            // lblQtyMasuk
            //
            this.lblQtyMasuk.AutoSize = true;
            this.lblQtyMasuk.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblQtyMasuk.Location = new Point(340, 156);
            this.lblQtyMasuk.Name = "lblQtyMasuk";
            this.lblQtyMasuk.Size = new Size(75, 19);
            this.lblQtyMasuk.Text = "Qty masuk:";
            //
            // numQtyMasuk
            //
            this.numQtyMasuk.Font = new Font("Segoe UI", 10F);
            this.numQtyMasuk.Location = new Point(340, 180);
            this.numQtyMasuk.Maximum = new decimal(new int[] { 99999, 0, 0, 0 });
            this.numQtyMasuk.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numQtyMasuk.Name = "numQtyMasuk";
            this.numQtyMasuk.Size = new Size(100, 36);
            this.numQtyMasuk.Value = new decimal(new int[] { 1, 0, 0, 0 });
            //
            // lblNilai
            //
            this.lblNilai.AutoSize = true;
            this.lblNilai.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblNilai.Location = new Point(20, 228);
            this.lblNilai.Name = "lblNilai";
            this.lblNilai.Size = new Size(120, 19);
            this.lblNilai.Text = "Nilai tukar (Rp):";
            //
            // txtNilai
            //
            this.txtNilai.Font = new Font("Segoe UI", 12F);
            this.txtNilai.Location = new Point(20, 252);
            this.txtNilai.Name = "txtNilai";
            this.txtNilai.PlaceholderText = "Contoh: 50000";
            this.txtNilai.Size = new Size(420, 42);
            //
            // lblWarning
            //
            this.lblWarning.AutoSize = false;
            this.lblWarning.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblWarning.Location = new Point(20, 302);
            this.lblWarning.Name = "lblWarning";
            this.lblWarning.Size = new Size(420, 24);
            this.lblWarning.Visible = false;
            //
            // btnKonfirmasi
            //
            this.btnKonfirmasi.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            this.btnKonfirmasi.Location = new Point(20, 340);
            this.btnKonfirmasi.Name = "btnKonfirmasi";
            this.btnKonfirmasi.Size = new Size(180, 46);
            this.btnKonfirmasi.Text = "Simpan";
            this.btnKonfirmasi.Click += new EventHandler(this.btnKonfirmasi_Click);
            //
            // btnHapus
            //
            this.btnHapus.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            this.btnHapus.Location = new Point(210, 340);
            this.btnHapus.Name = "btnHapus";
            this.btnHapus.Size = new Size(110, 46);
            this.btnHapus.Text = "Hapus";
            this.btnHapus.Visible = false;
            this.btnHapus.Click += new EventHandler(this.btnHapus_Click);
            //
            // btnBatal
            //
            this.btnBatal.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            this.btnBatal.Location = new Point(330, 340);
            this.btnBatal.Name = "btnBatal";
            this.btnBatal.Size = new Size(110, 46);
            this.btnBatal.Text = "Batal";
            this.btnBatal.Click += new EventHandler(this.btnBatal_Click);
            //
            // FormTukarTambah
            //
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(460, 410);
            this.Controls.Add(this.lblDeskripsi);
            this.Controls.Add(this.txtDeskripsi);
            this.Controls.Add(this.lblItemMasuk);
            this.Controls.Add(this.cmbItemMasuk);
            this.Controls.Add(this.lblQtyMasuk);
            this.Controls.Add(this.numQtyMasuk);
            this.Controls.Add(this.lblNilai);
            this.Controls.Add(this.txtNilai);
            this.Controls.Add(this.lblWarning);
            this.Controls.Add(this.btnKonfirmasi);
            this.Controls.Add(this.btnHapus);
            this.Controls.Add(this.btnBatal);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormTukarTambah";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Tukar Tambah";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void TerapkanTema()
        {
            Theme.StyleForm(this);
            formElipse = Theme.ApplyRoundedForm(this);
            Theme.StyleTextBox(txtDeskripsi);
            Theme.StyleTextBox(txtNilai);
            Theme.StyleComboBox(cmbItemMasuk);
            Theme.StyleNumericUpDown(numQtyMasuk);
            Theme.StylePrimaryButton(btnKonfirmasi);
            Theme.StyleDangerButton(btnHapus);
            Theme.StyleGhostButton(btnBatal);
            lblDeskripsi.ForeColor = Theme.Text;
            lblItemMasuk.ForeColor = Theme.Text;
            lblQtyMasuk.ForeColor = Theme.Text;
            lblNilai.ForeColor = Theme.Text;
            lblWarning.ForeColor = Theme.Danger;
            var header = Theme.BuildHeader("Tukar Tambah",
                "Barang lama ditukar — nilainya mengurangi total");
            this.Controls.Add(header);
        }

        // Item masuk hanya yang melacak stok (Fisik / Rakitan) — stoknya
        // yang akan naik. Baris pertama "(Tidak ada)" = barang umum tanpa
        // item di database (hanya nilainya yang dicatat).
        private void LoadItemMasuk()
        {
            try
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("ID_Item", typeof(int));
                dt.Columns.Add("Nama", typeof(string));
                DataRow kosong = dt.NewRow();
                kosong["ID_Item"] = 0;
                kosong["Nama"] = "(Tidak ada — barang umum)";
                dt.Rows.Add(kosong);
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Item, Nama FROM Item " +
                        "WHERE Tipe IN ('Fisik', 'Rakitan') ORDER BY Nama", conn))
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DataRow baris = dt.NewRow();
                            baris["ID_Item"] = Convert.ToInt32(reader["ID_Item"]);
                            baris["Nama"] = Convert.ToString(reader["Nama"]);
                            dt.Rows.Add(baris);
                        }
                    }
                }
                cmbItemMasuk.DisplayMember = "Nama";
                cmbItemMasuk.ValueMember = "ID_Item";
                cmbItemMasuk.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat item:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        private void btnKonfirmasi_Click(object sender, EventArgs e)
        {
            if (txtDeskripsi.Text.Trim().Length == 0)
            {
                lblWarning.Text = "Deskripsi barang wajib diisi.";
                lblWarning.Visible = true;
                return;
            }
            if (!TryParseDecimal(txtNilai.Text, out decimal nilai) || nilai <= 0)
            {
                lblWarning.Text = "Nilai tukar harus angka lebih dari 0.";
                lblWarning.Visible = true;
                return;
            }
            if (nilai > MaxNilai)
            {
                lblWarning.Text = "Nilai tukar tidak boleh melebihi Rp " +
                    MaxNilai.ToString("N0") + " (total setelah diskon & poin).";
                lblWarning.Visible = true;
                return;
            }
            int idItem = Convert.ToInt32(cmbItemMasuk.SelectedValue);
            if (idItem != 0 && numQtyMasuk.Value < 1)
            {
                lblWarning.Text = "Qty masuk minimal 1 kalau item dipilih.";
                lblWarning.Visible = true;
                return;
            }
            Deskripsi = txtDeskripsi.Text.Trim();
            IdItemMasuk = idItem == 0 ? (int?)null : idItem;
            QtyMasuk = idItem == 0 ? 0 : numQtyMasuk.Value;
            NilaiTukar = nilai;
            HapusDipilih = false;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnHapus_Click(object sender, EventArgs e)
        {
            HapusDipilih = true;
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
