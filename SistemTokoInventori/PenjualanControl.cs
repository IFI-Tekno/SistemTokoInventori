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
    /// Modul Penjualan lanjutan (Fase 2): Pesanan Penjualan (SO) + Piutang.
    /// SO: Draft -&gt; Diproses (saat dimuat ke Kasir) -&gt; Selesai (saat
    /// transaksi Kasir dari SO itu selesai). "Muat ke Kasir" hanya boleh
    /// kalau keranjang Kasir kosong; kalau transaksi dibatalkan, SO tetap
    /// 'Diproses' dan bisa dimuat ulang. Piutang: dibuat otomatis dari
    /// pembayaran Tempo di Kasir (sisa = Total - Bayar), lalu bisa dicatat
    /// pembayarannya (jumlah &lt;= sisa, status jadi 'Lunas' kalau sisa 0)
    /// + detail riwayat bayar.
    /// UI dibangun dari kode (tanpa Designer) seperti MasterDataControl.
    /// </summary>
    public partial class PenjualanControl : UserControl
    {
        // Shell (FormMain) — diset FormMain saat kontrol pertama dibuat.
        // Dipakai "Muat ke Kasir" untuk mengakses KasirControl + pindah tab.
        public FormMain Shell { get; set; }

        private Guna2TabControl tabControl;
        private TabPage tabSO;
        private TabPage tabPiutang;

        // ----- SO -----
        private Guna2DataGridView dgvSO;
        private Guna2TextBox txtCariSO;
        private Guna2ComboBox cmbPelangganSO;
        private Guna2ComboBox cmbItemSO;
        private Guna2NumericUpDown numQtySO;
        private Guna2TextBox txtHargaJualSO;
        private Guna2TextBox txtDiskonItemSO;
        private Guna2Button btnTambahItemSO;
        private Guna2DataGridView dgvSODetail;
        private Label lblTotalSO;
        private Guna2TextBox txtCatatanSO;
        private Guna2Button btnSimpanSO;
        private Guna2Button btnBatalSO;
        private Guna2Button btnMuatKasir;
        private Guna2Button btnClearSO;
        private DataTable soTable;
        private DataTable soDetailTable;
        private DataTable itemSOTable;
        // SO yang sedang dibuka di form (null = SO baru). Hanya Draft yang
        // boleh diubah / dibatalkan / dimuat ke Kasir (Diproses boleh dimuat
        // ulang — lihat btnMuatKasir_Click).
        private int? idSODipilih;
        private string statusSODipilih;
        private bool bolehEditSO = true;

        // ----- Piutang -----
        private Guna2TextBox txtCariPiutang;
        private Guna2DataGridView dgvPiutang;
        private Guna2Button btnBayarPiutang;
        private Guna2Button btnDetailPiutang;
        private DataTable piutangTable;

        public PenjualanControl()
        {
            InitializeComponent();
            TerapkanTema();
            LoadPelangganCombo();
            LoadItemCombo();
            LoadSO();
            LoadPiutang();
        }

        /// <summary>Dipanggil FormMain setiap kali tab Penjualan dibuka.</summary>
        public void RefreshData()
        {
            LoadPelangganCombo();
            LoadItemCombo();
            LoadSO();
            LoadPiutang();
        }

        // ==================== PEMBANGUN UI ====================
        private void InitializeComponent()
        {
            this.tabControl = new Guna.UI2.WinForms.Guna2TabControl();
            this.tabSO = new System.Windows.Forms.TabPage();
            this.tabPiutang = new System.Windows.Forms.TabPage();
            this.tabControl.Controls.Add(this.tabSO);
            this.tabControl.Controls.Add(this.tabPiutang);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Name = "tabControl";
            this.tabSO.Text = "Pesanan Penjualan";
            this.tabPiutang.Text = "Piutang";

            BangunTabSO();
            BangunTabPiutang();

            this.Controls.Add(this.tabControl);
            this.Name = "PenjualanControl";
            this.Size = new System.Drawing.Size(900, 620);
            var header = Theme.BuildHeader("Penjualan", "Pesanan penjualan (SO) dan piutang pelanggan");
            this.Controls.Add(header);
        }

        private Label BuatLabel(string teks, int x, int y)
        {
            Label lbl = new Label();
            lbl.AutoSize = true;
            lbl.Font = Theme.Font(10);
            lbl.Location = new Point(x, y);
            lbl.Text = teks;
            return lbl;
        }

        private Guna2TextBox BuatTextBox(string nama, int x, int y, int lebar, string placeholder)
        {
            Guna2TextBox txt = new Guna2TextBox();
            txt.Name = nama;
            txt.Font = Theme.Font(10);
            txt.Location = new Point(x, y);
            txt.Size = new Size(lebar, 30);
            txt.PlaceholderText = placeholder;
            return txt;
        }

        private Guna2Button BuatButton(string nama, string teks, int x, int y, EventHandler klik)
        {
            Guna2Button btn = new Guna2Button();
            btn.Name = nama;
            btn.Font = Theme.Font(10, FontStyle.Bold);
            btn.Location = new Point(x, y);
            btn.Size = new Size(120, 42);
            btn.Text = teks;
            btn.Click += klik;
            return btn;
        }

        private Guna2DataGridView BuatGrid(string nama, int tinggi)
        {
            Guna2DataGridView grid = new Guna2DataGridView();
            grid.Name = nama;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grid.Location = new Point(12, 56);
            grid.ReadOnly = true;
            grid.Size = new Size(828, tinggi);
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            return grid;
        }

        private Guna2TextBox BuatCari(string nama, EventHandler berubah)
        {
            Guna2TextBox txt = BuatTextBox(nama, 12, 12, 300, "Cari...");
            txt.Size = new Size(300, 36);
            txt.TextChanged += berubah;
            return txt;
        }

        private void BangunTabSO()
        {
            txtCariSO = BuatCari("txtCariSO", txtCariSO_TextChanged);
            txtCariSO.PlaceholderText = "Cari No SO / pelanggan...";
            dgvSO = BuatGrid("dgvSO", 150);
            dgvSO.CellClick += dgvSO_CellClick;

            cmbPelangganSO = new Guna2ComboBox();
            cmbPelangganSO.Name = "cmbPelangganSO";
            cmbPelangganSO.Font = Theme.Font(10);
            cmbPelangganSO.Location = new Point(12, 234);
            cmbPelangganSO.Size = new Size(200, 30);
            cmbPelangganSO.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbItemSO = new Guna2ComboBox();
            cmbItemSO.Name = "cmbItemSO";
            cmbItemSO.Font = Theme.Font(10);
            cmbItemSO.Location = new Point(230, 234);
            cmbItemSO.Size = new Size(200, 30);
            cmbItemSO.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbItemSO.SelectedIndexChanged += cmbItemSO_SelectedIndexChanged;
            numQtySO = new Guna2NumericUpDown();
            numQtySO.Name = "numQtySO";
            numQtySO.Font = Theme.Font(10);
            numQtySO.Location = new Point(450, 234);
            numQtySO.Size = new Size(70, 30);
            numQtySO.Minimum = 1;
            numQtySO.Maximum = 99999;
            numQtySO.Value = 1;
            txtHargaJualSO = BuatTextBox("txtHargaJualSO", 530, 234, 110, "0");
            txtDiskonItemSO = BuatTextBox("txtDiskonItemSO", 650, 234, 70, "0");
            btnTambahItemSO = BuatButton("btnTambahItemSO", "Tambah", 730, 234, btnTambahItemSO_Click);
            btnTambahItemSO.Size = new Size(110, 36);

            soDetailTable = new DataTable();
            soDetailTable.Columns.Add("ID_Item", typeof(int));
            soDetailTable.Columns.Add("Nama", typeof(string));
            soDetailTable.Columns.Add("Qty", typeof(decimal));
            soDetailTable.Columns.Add("HargaJual", typeof(decimal));
            soDetailTable.Columns.Add("DiskonItem", typeof(decimal));
            soDetailTable.Columns.Add("Subtotal", typeof(decimal));
            dgvSODetail = BuatGrid("dgvSODetail", 100);
            dgvSODetail.Location = new Point(12, 278);
            dgvSODetail.DataSource = soDetailTable;
            dgvSODetail.CellDoubleClick += dgvSODetail_CellDoubleClick;
            if (dgvSODetail.Columns["ID_Item"] != null)
                dgvSODetail.Columns["ID_Item"].Visible = false;

            lblTotalSO = BuatLabel("Total SO: Rp 0", 12, 384);
            lblTotalSO.Font = Theme.Font(11, FontStyle.Bold);
            txtCatatanSO = BuatTextBox("txtCatatanSO", 12, 428, 300, "Catatan (opsional)");

            btnSimpanSO = BuatButton("btnSimpanSO", "Simpan SO", 12, 468, btnSimpanSO_Click);
            btnBatalSO = BuatButton("btnBatalSO", "Batalkan", 144, 468, btnBatalSO_Click);
            btnMuatKasir = BuatButton("btnMuatKasir", "Muat ke Kasir", 276, 468, btnMuatKasir_Click);
            btnMuatKasir.Size = new Size(150, 42);
            btnClearSO = BuatButton("btnClearSO", "Baru", 438, 468, btnClearSO_Click);

            tabSO.Controls.Add(txtCariSO);
            tabSO.Controls.Add(dgvSO);
            tabSO.Controls.Add(BuatLabel("Pelanggan", 12, 214));
            tabSO.Controls.Add(BuatLabel("Item", 230, 214));
            tabSO.Controls.Add(BuatLabel("Qty", 450, 214));
            tabSO.Controls.Add(BuatLabel("Harga Jual", 530, 214));
            tabSO.Controls.Add(BuatLabel("Diskon", 650, 214));
            tabSO.Controls.Add(cmbPelangganSO);
            tabSO.Controls.Add(cmbItemSO);
            tabSO.Controls.Add(numQtySO);
            tabSO.Controls.Add(txtHargaJualSO);
            tabSO.Controls.Add(txtDiskonItemSO);
            tabSO.Controls.Add(btnTambahItemSO);
            tabSO.Controls.Add(dgvSODetail);
            tabSO.Controls.Add(lblTotalSO);
            tabSO.Controls.Add(BuatLabel("Catatan", 12, 408));
            tabSO.Controls.Add(txtCatatanSO);
            tabSO.Controls.Add(btnSimpanSO);
            tabSO.Controls.Add(btnBatalSO);
            tabSO.Controls.Add(btnMuatKasir);
            tabSO.Controls.Add(btnClearSO);
            PerbaruiTombolSO();
        }

        private void BangunTabPiutang()
        {
            txtCariPiutang = BuatCari("txtCariPiutang", txtCariPiutang_TextChanged);
            txtCariPiutang.PlaceholderText = "Cari No faktur / pelanggan...";
            dgvPiutang = BuatGrid("dgvPiutang", 380);
            dgvPiutang.CellFormatting += dgvPiutang_CellFormatting;
            btnBayarPiutang = BuatButton("btnBayarPiutang", "Catat Pembayaran", 12, 452, btnBayarPiutang_Click);
            btnBayarPiutang.Size = new Size(180, 42);
            btnDetailPiutang = BuatButton("btnDetailPiutang", "Detail", 204, 452, btnDetailPiutang_Click);

            tabPiutang.Controls.Add(txtCariPiutang);
            tabPiutang.Controls.Add(dgvPiutang);
            tabPiutang.Controls.Add(btnBayarPiutang);
            tabPiutang.Controls.Add(btnDetailPiutang);
        }

        private void TerapkanTema()
        {
            this.BackColor = Theme.Background;
            this.Font = Theme.Font(10);
            Theme.StyleTabControl(tabControl);
            Theme.StyleGrid(dgvSO);
            Theme.StyleGrid(dgvSODetail);
            Theme.StyleGrid(dgvPiutang);
            Theme.StylePrimaryButton(btnSimpanSO);
            Theme.StyleDangerButton(btnBatalSO);
            Theme.StyleSuccessButton(btnMuatKasir);
            Theme.StyleGhostButton(btnClearSO);
            Theme.StylePrimaryButton(btnTambahItemSO);
            Theme.StylePrimaryButton(btnBayarPiutang);
            Theme.StyleGhostButton(btnDetailPiutang);
            Theme.StyleComboBox(cmbPelangganSO);
            Theme.StyleComboBox(cmbItemSO);
            Theme.StyleNumericUpDown(numQtySO);
            Theme.StyleTextBox(txtHargaJualSO);
            Theme.StyleTextBox(txtDiskonItemSO);
            Theme.StyleTextBox(txtCatatanSO);
            Theme.StyleTextBox(txtCariSO);
            Theme.StyleTextBox(txtCariPiutang);
            lblTotalSO.ForeColor = Theme.Text;
        }

        // ==================== HELPER ====================
        private static bool TryParseDecimal(string teks, out decimal nilai)
        {
            teks = (teks ?? "").Trim();
            if (decimal.TryParse(teks, NumberStyles.Number, CultureInfo.CurrentCulture, out nilai))
                return true;
            return decimal.TryParse(teks, NumberStyles.Number, CultureInfo.InvariantCulture, out nilai);
        }

        private static string EscapeFilter(string kata)
        {
            kata = kata.Replace("'", "''");
            kata = kata.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
            return kata;
        }

        // ==================== SO: MUAT DATA ====================
        private void LoadPelangganCombo()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Pelanggan, Nama FROM Pelanggan ORDER BY Nama", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        cmbPelangganSO.DisplayMember = "Nama";
                        cmbPelangganSO.ValueMember = "ID_Pelanggan";
                        cmbPelangganSO.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat pelanggan:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadItemCombo()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Item, Nama, HargaJual FROM Item ORDER BY Nama", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        itemSOTable = new DataTable();
                        adapter.Fill(itemSOTable);
                        cmbItemSO.DisplayMember = "Nama";
                        cmbItemSO.ValueMember = "ID_Item";
                        cmbItemSO.DataSource = itemSOTable;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat item:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSO()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT so.ID_SO, so.NoSO, p.Nama AS Pelanggan, " +
                        "so.Tanggal, so.Total, so.Status FROM SalesOrder so " +
                        "LEFT JOIN Pelanggan p ON p.ID_Pelanggan = so.ID_Pelanggan " +
                        "ORDER BY so.ID_SO DESC", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        soTable = new DataTable();
                        adapter.Fill(soTable);
                        dgvSO.DataSource = soTable;
                        if (dgvSO.Columns["ID_SO"] != null)
                            dgvSO.Columns["ID_SO"].Visible = false;
                        if (dgvSO.Columns["Total"] != null)
                            dgvSO.Columns["Total"].DefaultCellStyle.Format = "N0";
                        TerapkanFilterSO();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat SO:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TerapkanFilterSO()
        {
            if (soTable == null) return;
            string kata = EscapeFilter(txtCariSO.Text.Trim());
            soTable.DefaultView.RowFilter = kata.Length == 0
                ? ""
                : "NoSO LIKE '%" + kata + "%' OR Pelanggan LIKE '%" + kata + "%'";
        }

        private void txtCariSO_TextChanged(object sender, EventArgs e)
        {
            TerapkanFilterSO();
        }

        private void cmbItemSO_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbItemSO.SelectedValue == null || itemSOTable == null) return;
            foreach (DataRow baris in itemSOTable.Rows)
            {
                if (Convert.ToInt32(baris["ID_Item"]) == Convert.ToInt32(cmbItemSO.SelectedValue))
                {
                    txtHargaJualSO.Text = Convert.ToDecimal(baris["HargaJual"]).ToString("N0");
                    return;
                }
            }
        }

        private decimal HitungTotalSO()
        {
            decimal total = 0;
            foreach (DataRow baris in soDetailTable.Rows)
                total += Convert.ToDecimal(baris["Subtotal"]);
            lblTotalSO.Text = "Total SO: Rp " + total.ToString("N0");
            return total;
        }

        // ==================== SO: FORM DETAIL ====================
        private void btnTambahItemSO_Click(object sender, EventArgs e)
        {
            if (!bolehEditSO)
            {
                MessageBox.Show("SO ini sudah tidak bisa diubah (status " + statusSODipilih + ").");
                return;
            }
            if (cmbItemSO.SelectedValue == null)
            {
                MessageBox.Show("Pilih item dulu!");
                return;
            }
            decimal qty = numQtySO.Value;
            if (qty <= 0)
            {
                MessageBox.Show("Qty minimal 1!");
                return;
            }
            if (!TryParseDecimal(txtHargaJualSO.Text, out decimal hargaJual) || hargaJual <= 0)
            {
                MessageBox.Show("Harga jual harus angka lebih dari 0!");
                return;
            }
            decimal diskonItem = 0;
            if (txtDiskonItemSO.Text.Trim().Length > 0)
            {
                if (!TryParseDecimal(txtDiskonItemSO.Text, out diskonItem) || diskonItem < 0)
                {
                    MessageBox.Show("Diskon item harus angka 0 atau lebih!");
                    return;
                }
            }
            if (diskonItem >= hargaJual)
            {
                MessageBox.Show("Diskon item tidak boleh >= harga jual!");
                return;
            }
            int idItem = Convert.ToInt32(cmbItemSO.SelectedValue);
            string nama = Convert.ToString(cmbItemSO.Text);
            DataRow ada = null;
            foreach (DataRow baris in soDetailTable.Rows)
            {
                if (Convert.ToInt32(baris["ID_Item"]) == idItem) { ada = baris; break; }
            }
            if (ada != null)
            {
                ada["Qty"] = Convert.ToDecimal(ada["Qty"]) + qty;
                ada["HargaJual"] = hargaJual;
                ada["DiskonItem"] = diskonItem;
                ada["Subtotal"] = (hargaJual - diskonItem) * Convert.ToDecimal(ada["Qty"]);
            }
            else
            {
                DataRow baris = soDetailTable.NewRow();
                baris["ID_Item"] = idItem;
                baris["Nama"] = nama;
                baris["Qty"] = qty;
                baris["HargaJual"] = hargaJual;
                baris["DiskonItem"] = diskonItem;
                baris["Subtotal"] = (hargaJual - diskonItem) * qty;
                soDetailTable.Rows.Add(baris);
            }
            HitungTotalSO();
            numQtySO.Value = 1;
            txtDiskonItemSO.Text = "0";
        }

        private void dgvSODetail_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || !bolehEditSO) return;
            if (MessageBox.Show("Hapus baris ini dari SO?", "Konfirmasi",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                soDetailTable.Rows.RemoveAt(e.RowIndex);
                HitungTotalSO();
            }
        }

        private void dgvSO_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = dgvSO.Rows[e.RowIndex];
            int id = Convert.ToInt32(row.Cells["ID_SO"].Value);
            MuatSOkeForm(id);
        }

        private void MuatSOkeForm(int idSO)
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Pelanggan, Total, Catatan, Status " +
                        "FROM SalesOrder WHERE ID_SO = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idSO);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read()) return;
                            idSODipilih = idSO;
                            statusSODipilih = Convert.ToString(reader["Status"]);
                            object pel = reader["ID_Pelanggan"];
                            cmbPelangganSO.SelectedValue = (pel == null || pel == DBNull.Value)
                                ? null : pel;
                            txtCatatanSO.Text = Convert.ToString(reader["Catatan"]);
                        }
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT d.ID_Item, i.Nama, d.Qty, d.HargaJual, d.DiskonItem, d.Subtotal " +
                        "FROM SODetail d INNER JOIN Item i ON i.ID_Item = d.ID_Item " +
                        "WHERE d.ID_SO = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idSO);
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        soDetailTable.Clear();
                        adapter.Fill(soDetailTable);
                    }
                }
                HitungTotalSO();
                AturEditSO(statusSODipilih == "Draft");
                PerbaruiTombolSO();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat SO:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AturEditSO(bool boleh)
        {
            bolehEditSO = boleh;
            cmbPelangganSO.Enabled = boleh;
            cmbItemSO.Enabled = boleh;
            numQtySO.Enabled = boleh;
            txtHargaJualSO.Enabled = boleh;
            txtDiskonItemSO.Enabled = boleh;
            btnTambahItemSO.Enabled = boleh;
            txtCatatanSO.Enabled = boleh;
        }

        private void PerbaruiTombolSO()
        {
            if (idSODipilih == null)
            {
                btnSimpanSO.Enabled = true;
                btnBatalSO.Enabled = false;
                btnMuatKasir.Enabled = false;
                return;
            }
            switch (statusSODipilih)
            {
                case "Draft":
                    btnSimpanSO.Enabled = true;
                    btnBatalSO.Enabled = true;
                    btnMuatKasir.Enabled = true;
                    break;
                case "Diproses":
                    // Sudah dimuat ke Kasir tapi transaksi belum selesai:
                    // tidak bisa diubah, tapi boleh dimuat ulang.
                    btnSimpanSO.Enabled = false;
                    btnBatalSO.Enabled = false;
                    btnMuatKasir.Enabled = true;
                    break;
                default: // Selesai / Dibatalkan
                    btnSimpanSO.Enabled = false;
                    btnBatalSO.Enabled = false;
                    btnMuatKasir.Enabled = false;
                    break;
            }
        }

        private void ResetFormSO()
        {
            idSODipilih = null;
            statusSODipilih = null;
            soDetailTable.Clear();
            txtCatatanSO.Text = "";
            HitungTotalSO();
            AturEditSO(true);
            PerbaruiTombolSO();
        }

        private void btnClearSO_Click(object sender, EventArgs e)
        {
            ResetFormSO();
        }

        // ==================== SO: SIMPAN / BATAL / MUAT ====================
        private string BuatNoSO()
        {
            string tahun = DateTime.Now.ToString("yyyy");
            long jumlah = 0;
            using (var conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT COUNT(*) FROM SalesOrder WHERE NoSO LIKE @Pola", conn))
                {
                    cmd.Parameters.AddWithValue("@Pola", "SO-" + tahun + "-%");
                    jumlah = Convert.ToInt64(cmd.ExecuteScalar());
                }
            }
            return "SO-" + tahun + "-" + (jumlah + 1).ToString("D4");
        }

        private void btnSimpanSO_Click(object sender, EventArgs e)
        {
            if (!bolehEditSO)
            {
                MessageBox.Show("SO ini sudah tidak bisa diubah (status " + statusSODipilih + ").");
                return;
            }
            if (cmbPelangganSO.SelectedValue == null)
            {
                MessageBox.Show("Pilih pelanggan dulu!");
                return;
            }
            if (soDetailTable.Rows.Count == 0)
            {
                MessageBox.Show("Tambahkan minimal 1 item dulu!");
                return;
            }
            decimal total = HitungTotalSO();
            int idPelanggan = Convert.ToInt32(cmbPelangganSO.SelectedValue);
            string catatan = txtCatatanSO.Text.Trim();
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        if (idSODipilih == null)
                        {
                            using (var cmd = new SQLiteCommand(
                                "INSERT INTO SalesOrder (NoSO, ID_Pelanggan, Tanggal, " +
                                "Status, Total, Catatan) " +
                                "VALUES (@NoSO, @ID_Pelanggan, @Tanggal, 'Draft', " +
                                "@Total, @Catatan)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@NoSO", BuatNoSO());
                                cmd.Parameters.AddWithValue("@ID_Pelanggan", idPelanggan);
                                cmd.Parameters.AddWithValue("@Tanggal", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                cmd.Parameters.AddWithValue("@Total", total);
                                cmd.Parameters.AddWithValue("@Catatan", catatan);
                                cmd.ExecuteNonQuery();
                            }
                            using (var cmd = new SQLiteCommand("SELECT last_insert_rowid()", conn, trans))
                            {
                                idSODipilih = (int)(long)cmd.ExecuteScalar();
                            }
                        }
                        else
                        {
                            using (var cmd = new SQLiteCommand(
                                "UPDATE SalesOrder SET ID_Pelanggan = @ID_Pelanggan, " +
                                "Total = @Total, Catatan = @Catatan " +
                                "WHERE ID_SO = @ID AND Status = 'Draft'", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID_Pelanggan", idPelanggan);
                                cmd.Parameters.AddWithValue("@Total", total);
                                cmd.Parameters.AddWithValue("@Catatan", catatan);
                                cmd.Parameters.AddWithValue("@ID", idSODipilih.Value);
                                if (cmd.ExecuteNonQuery() == 0)
                                {
                                    trans.Rollback();
                                    MessageBox.Show("SO sudah tidak Draft — tidak bisa diubah.");
                                    return;
                                }
                            }
                            using (var cmd = new SQLiteCommand(
                                "DELETE FROM SODetail WHERE ID_SO = @ID", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID", idSODipilih.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        foreach (DataRow baris in soDetailTable.Rows)
                        {
                            using (var cmd = new SQLiteCommand(
                                "INSERT INTO SODetail (ID_SO, ID_Item, Qty, HargaJual, DiskonItem, Subtotal) " +
                                "VALUES (@ID_SO, @ID_Item, @Qty, @HargaJual, @DiskonItem, @Subtotal)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID_SO", idSODipilih.Value);
                                cmd.Parameters.AddWithValue("@ID_Item", baris["ID_Item"]);
                                cmd.Parameters.AddWithValue("@Qty", baris["Qty"]);
                                cmd.Parameters.AddWithValue("@HargaJual", baris["HargaJual"]);
                                cmd.Parameters.AddWithValue("@DiskonItem", baris["DiskonItem"]);
                                cmd.Parameters.AddWithValue("@Subtotal", baris["Subtotal"]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        trans.Commit();
                    }
                }
                int idSimpan = idSODipilih.Value;
                LoadSO();
                MuatSOkeForm(idSimpan);
                MessageBox.Show("SO tersimpan (Draft).");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menyimpan SO:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnBatalSO_Click(object sender, EventArgs e)
        {
            if (idSODipilih == null) return;
            if (MessageBox.Show("Batalkan SO ini?", "Konfirmasi",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "UPDATE SalesOrder SET Status = 'Dibatalkan' " +
                        "WHERE ID_SO = @ID AND Status = 'Draft'", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idSODipilih.Value);
                        if (cmd.ExecuteNonQuery() == 0)
                        {
                            MessageBox.Show("SO sudah tidak Draft — tidak bisa dibatalkan.");
                            return;
                        }
                    }
                }
                LoadSO();
                ResetFormSO();
                MessageBox.Show("SO dibatalkan.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal membatalkan SO:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Muat SO ke keranjang Kasir. Syarat: keranjang Kasir KOSONG
        // (kalau tidak, tolak — jangan campur SO dengan belanja lain),
        // dan SO masih Draft / Diproses. Status jadi 'Diproses', lalu
        // shell pindah ke tab Kasir. Kalau transaksi Kasir selesai, SO
        // jadi 'Selesai' (di KasirControl langkah 2). Kalau transaksi
        // dibatalkan, SO tetap 'Diproses' dan bisa dimuat ulang — tapi
        // keranjang harus dikosongkan dulu.
        private void btnMuatKasir_Click(object sender, EventArgs e)
        {
            if (idSODipilih == null)
            {
                MessageBox.Show("Pilih SO dulu yang mau dimuat ke Kasir!");
                return;
            }
            if (Shell == null)
            {
                MessageBox.Show("Shell belum diset — tidak bisa pindah ke Kasir.");
                return;
            }
            if (statusSODipilih != "Draft" && statusSODipilih != "Diproses")
            {
                MessageBox.Show("SO sudah " + statusSODipilih + " — tidak bisa dimuat ke Kasir.");
                return;
            }
            if (!Shell.Kasir.KeranjangKosong)
            {
                MessageBox.Show("Keranjang Kasir tidak kosong!\n" +
                    "Kosongkan dulu keranjang sebelum memuat SO.");
                return;
            }
            try
            {
                int? idPelangganSO = null;
                DataTable detail = new DataTable();
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Pelanggan FROM SalesOrder WHERE ID_SO = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idSODipilih.Value);
                        object hasil = cmd.ExecuteScalar();
                        if (hasil != null && hasil != DBNull.Value)
                            idPelangganSO = Convert.ToInt32(hasil);
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT d.ID_Item, i.Nama, d.HargaJual, d.DiskonItem, d.Qty, i.Tipe " +
                        "FROM SODetail d INNER JOIN Item i ON i.ID_Item = d.ID_Item " +
                        "WHERE d.ID_SO = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idSODipilih.Value);
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        adapter.Fill(detail);
                    }
                    if (detail.Rows.Count == 0)
                    {
                        MessageBox.Show("SO ini tidak punya item.");
                        return;
                    }
                    using (var cmd = new SQLiteCommand(
                        "UPDATE SalesOrder SET Status = 'Diproses' " +
                        "WHERE ID_SO = @ID AND Status IN ('Draft', 'Diproses')", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idSODipilih.Value);
                        if (cmd.ExecuteNonQuery() == 0)
                        {
                            MessageBox.Show("SO sudah tidak bisa dimuat (status berubah).");
                            return;
                        }
                    }
                }
                Shell.Kasir.MuatSalesOrder(idSODipilih.Value, idPelangganSO, detail);
                LoadSO();
                Shell.TampilkanKasir();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat SO ke Kasir:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==================== PIUTANG ====================
        private void LoadPiutang()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT p.ID_Piutang, p.NoFaktur, c.Nama AS Pelanggan, " +
                        "p.Tanggal, p.JatuhTempo, p.Total, p.Sisa, p.Status FROM Piutang p " +
                        "LEFT JOIN Pelanggan c ON c.ID_Pelanggan = p.ID_Pelanggan " +
                        "ORDER BY p.ID_Piutang DESC", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        piutangTable = new DataTable();
                        adapter.Fill(piutangTable);
                        dgvPiutang.DataSource = piutangTable;
                        if (dgvPiutang.Columns["ID_Piutang"] != null)
                            dgvPiutang.Columns["ID_Piutang"].Visible = false;
                        if (dgvPiutang.Columns["Total"] != null)
                            dgvPiutang.Columns["Total"].DefaultCellStyle.Format = "N0";
                        if (dgvPiutang.Columns["Sisa"] != null)
                            dgvPiutang.Columns["Sisa"].DefaultCellStyle.Format = "N0";
                        TerapkanFilterPiutang();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat piutang:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TerapkanFilterPiutang()
        {
            if (piutangTable == null) return;
            string kata = EscapeFilter(txtCariPiutang.Text.Trim());
            piutangTable.DefaultView.RowFilter = kata.Length == 0
                ? ""
                : "NoFaktur LIKE '%" + kata + "%' OR Pelanggan LIKE '%" + kata + "%'";
        }

        private void txtCariPiutang_TextChanged(object sender, EventArgs e)
        {
            TerapkanFilterPiutang();
        }

        // Baris jatuh tempo merah (teks) kalau sudah lewat dan belum lunas.
        private void dgvPiutang_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvPiutang.Columns[e.ColumnIndex].Name != "JatuhTempo") return;
            DataGridViewRow row = dgvPiutang.Rows[e.RowIndex];
            if (Convert.ToString(row.Cells["Status"].Value) != "Belum Lunas") return;
            DateTime jatuh;
            if (DateTime.TryParse(Convert.ToString(row.Cells["JatuhTempo"].Value), out jatuh)
                && jatuh.Date < DateTime.Now.Date)
            {
                e.CellStyle.ForeColor = Theme.Danger;
                e.CellStyle.Font = Theme.Font(10, FontStyle.Bold);
            }
        }

        private void btnBayarPiutang_Click(object sender, EventArgs e)
        {
            if (dgvPiutang.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih piutang dulu yang mau dibayar!");
                return;
            }
            DataGridViewRow row = dgvPiutang.SelectedRows[0];
            int idPiutang = Convert.ToInt32(row.Cells["ID_Piutang"].Value);
            decimal sisa = Convert.ToDecimal(row.Cells["Sisa"].Value);
            decimal total = Convert.ToDecimal(row.Cells["Total"].Value);
            string pelanggan = Convert.ToString(row.Cells["Pelanggan"].Value);
            string noFaktur = Convert.ToString(row.Cells["NoFaktur"].Value);
            string status = Convert.ToString(row.Cells["Status"].Value);
            if (status == "Lunas")
            {
                MessageBox.Show("Piutang ini sudah lunas.");
                return;
            }
            FormCatatPembayaran form = new FormCatatPembayaran();
            form.Judul = "Catat Pembayaran Piutang";
            form.Konteks = pelanggan + " — " + noFaktur;
            form.Sisa = sisa;
            if (form.ShowDialog() != DialogResult.OK)
                return;
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        decimal sisaSekarang;
                        using (var cmd = new SQLiteCommand(
                            "SELECT Sisa FROM Piutang WHERE ID_Piutang = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", idPiutang);
                            sisaSekarang = Convert.ToDecimal(cmd.ExecuteScalar());
                        }
                        if (form.Jumlah > sisaSekarang)
                        {
                            trans.Rollback();
                            MessageBox.Show("Jumlah melebihi sisa piutang Rp " +
                                sisaSekarang.ToString("N0") + ".");
                            return;
                        }
                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO PiutangBayar (ID_Piutang, Tanggal, Jumlah, Keterangan) " +
                            "VALUES (@ID_Piutang, @Tanggal, @Jumlah, @Keterangan)", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID_Piutang", idPiutang);
                            cmd.Parameters.AddWithValue("@Tanggal", form.Tanggal.ToString("yyyy-MM-dd"));
                            cmd.Parameters.AddWithValue("@Jumlah", form.Jumlah);
                            cmd.Parameters.AddWithValue("@Keterangan", form.Keterangan);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand(
                            "UPDATE Piutang SET Sisa = Sisa - @Jumlah, " +
                            "Status = CASE WHEN Sisa - @Jumlah <= 0 THEN 'Lunas' " +
                            "ELSE 'Belum Lunas' END " +
                            "WHERE ID_Piutang = @ID AND Sisa >= @Jumlah", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Jumlah", form.Jumlah);
                            cmd.Parameters.AddWithValue("@ID", idPiutang);
                            if (cmd.ExecuteNonQuery() == 0)
                            {
                                trans.Rollback();
                                MessageBox.Show("Pembayaran gagal — sisa piutang sudah berubah.");
                                return;
                            }
                        }
                        // Fase 4: jurnal terima piutang (dalam transaksi
                        // yang sama). Debit Kas [jumlah]; Kredit Piutang
                        // Usaha [jumlah].
                        JurnalHelper.TulisJurnalTerimaPiutang(conn, trans,
                            form.Tanggal.ToString("yyyy-MM-dd HH:mm:ss"),
                            form.Jumlah, idPiutang);
                        trans.Commit();
                    }
                }
                LoadPiutang();
                MessageBox.Show("Pembayaran Rp " + form.Jumlah.ToString("N0") + " tercatat.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mencatat pembayaran:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDetailPiutang_Click(object sender, EventArgs e)
        {
            if (dgvPiutang.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih piutang dulu yang mau dilihat detailnya!");
                return;
            }
            DataGridViewRow row = dgvPiutang.SelectedRows[0];
            int idPiutang = Convert.ToInt32(row.Cells["ID_Piutang"].Value);
            decimal sisa = Convert.ToDecimal(row.Cells["Sisa"].Value);
            decimal total = Convert.ToDecimal(row.Cells["Total"].Value);
            string pelanggan = Convert.ToString(row.Cells["Pelanggan"].Value);
            string noFaktur = Convert.ToString(row.Cells["NoFaktur"].Value);
            try
            {
                DataTable riwayat = new DataTable();
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT Tanggal, Jumlah, Keterangan FROM PiutangBayar " +
                        "WHERE ID_Piutang = @ID ORDER BY Tanggal DESC, ID_PiutangBayar DESC", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idPiutang);
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        adapter.Fill(riwayat);
                    }
                }
                FormDetailPembayaran form = new FormDetailPembayaran();
                form.Judul = "Detail Pembayaran Piutang";
                form.Konteks = pelanggan + " — " + noFaktur;
                form.Total = total;
                form.Sisa = sisa;
                form.Pembayaran = riwayat;
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat detail:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
