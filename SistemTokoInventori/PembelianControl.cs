using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace SistemTokoInventori
{
    /// <summary>
    /// Modul Pembelian (Fase 2): Pesanan Pembelian (PO) + Hutang.
    /// PO: Draft -&gt; Dikirim -&gt; Diterima. Saat diterima, dalam SATU
    /// transaksi SQLite: stok item (Fisik/Rakitan) bertambah, StokMovement
    /// 'Masuk' dicatat, dan kalau Total &gt; DP dibuat satu baris Hutang
    /// (jatuh tempo = tanggal terima + 30 hari). PO Diterima tidak bisa
    /// diubah / dibatalkan lagi. Hutang: catat pembayaran (jumlah &lt;= sisa,
    /// status jadi 'Lunas' kalau sisa 0) + detail riwayat bayar.
    /// UI dibangun dari kode (tanpa Designer) seperti MasterDataControl.
    /// </summary>
    public partial class PembelianControl : UserControl
    {
        private Guna2TabControl tabControl;
        private TabPage tabPO;
        private TabPage tabHutang;

        // ----- PO -----
        private Guna2DataGridView dgvPO;
        private Guna2TextBox txtCariPO;
        private Guna2ComboBox cmbSupplierPO;
        private Guna2ComboBox cmbItemPO;
        private Guna2NumericUpDown numQtyPO;
        private Guna2TextBox txtHargaBeliPO;
        private Guna2Button btnTambahItemPO;
        private Guna2DataGridView dgvPODetail;
        private Label lblTotalPO;
        private Guna2TextBox txtDPPO;
        private Guna2TextBox txtCatatanPO;
        private Guna2Button btnSimpanPO;
        private Guna2Button btnKirimPO;
        private Guna2Button btnTerimaPO;
        private Guna2Button btnBatalPO;
        private Guna2Button btnCetakPO;
        private Guna2Button btnClearPO;
        private DataTable poTable;
        private DataTable poDetailTable;
        private DataTable itemPOTable;
        // PO yang sedang dibuka di form (null = PO baru). statusPODipilih
        // null kalau PO baru; kalau tidak, 'Draft'/'Dikirim'/'Diterima'/
        // 'Dibatalkan'. Hanya Draft yang boleh diubah.
        private int? idPODipilih;
        private string statusPODipilih;
        private bool bolehEditPO = true;

        // ----- Hutang -----
        private Guna2TextBox txtCariHutang;
        private Guna2DataGridView dgvHutang;
        private Guna2Button btnBayarHutang;
        private Guna2Button btnDetailHutang;
        private DataTable hutangTable;

        // ----- Cetak PO -----
        private PrintDocument docPO;
        private DataTable printDetail;
        private string printNoPO = "";
        private string printSupplier = "";
        private string printTanggal = "";
        private decimal printTotal;
        private decimal printDP;
        private int printIndex;

        public PembelianControl()
        {
            InitializeComponent();
            TerapkanTema();
            LoadSupplierCombo();
            LoadItemCombo();
            LoadPO();
            LoadHutang();
        }

        /// <summary>Dipanggil FormMain setiap kali tab Pembelian dibuka.</summary>
        public void RefreshData()
        {
            LoadSupplierCombo();
            LoadItemCombo();
            LoadPO();
            LoadHutang();
        }

        // ==================== PEMBANGUN UI ====================
        private void InitializeComponent()
        {
            this.tabControl = new Guna.UI2.WinForms.Guna2TabControl();
            this.tabPO = new System.Windows.Forms.TabPage();
            this.tabHutang = new System.Windows.Forms.TabPage();
            this.tabControl.Controls.Add(this.tabPO);
            this.tabControl.Controls.Add(this.tabHutang);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Name = "tabControl";
            this.tabPO.Text = "Pesanan Pembelian";
            this.tabHutang.Text = "Hutang";

            BangunTabPO();
            BangunTabHutang();

            this.Controls.Add(this.tabControl);
            this.Name = "PembelianControl";
            this.Size = new System.Drawing.Size(900, 620);
            var header = Theme.BuildHeader("Pembelian", "Pesanan pembelian (PO) dan hutang ke supplier");
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

        private void BangunTabPO()
        {
            txtCariPO = BuatCari("txtCariPO", txtCariPO_TextChanged);
            txtCariPO.PlaceholderText = "Cari No PO / supplier...";
            dgvPO = BuatGrid("dgvPO", 150);
            dgvPO.CellClick += dgvPO_CellClick;

            cmbSupplierPO = new Guna2ComboBox();
            cmbSupplierPO.Name = "cmbSupplierPO";
            cmbSupplierPO.Font = Theme.Font(10);
            cmbSupplierPO.Location = new Point(12, 234);
            cmbSupplierPO.Size = new Size(200, 30);
            cmbSupplierPO.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbItemPO = new Guna2ComboBox();
            cmbItemPO.Name = "cmbItemPO";
            cmbItemPO.Font = Theme.Font(10);
            cmbItemPO.Location = new Point(230, 234);
            cmbItemPO.Size = new Size(200, 30);
            cmbItemPO.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbItemPO.SelectedIndexChanged += cmbItemPO_SelectedIndexChanged;
            numQtyPO = new Guna2NumericUpDown();
            numQtyPO.Name = "numQtyPO";
            numQtyPO.Font = Theme.Font(10);
            numQtyPO.Location = new Point(450, 234);
            numQtyPO.Size = new Size(70, 30);
            numQtyPO.Minimum = 1;
            numQtyPO.Maximum = 99999;
            numQtyPO.Value = 1;
            txtHargaBeliPO = BuatTextBox("txtHargaBeliPO", 530, 234, 110, "0");
            btnTambahItemPO = BuatButton("btnTambahItemPO", "Tambah Item", 660, 234, btnTambahItemPO_Click);
            btnTambahItemPO.Size = new Size(120, 36);

            poDetailTable = new DataTable();
            poDetailTable.Columns.Add("ID_Item", typeof(int));
            poDetailTable.Columns.Add("Nama", typeof(string));
            poDetailTable.Columns.Add("Qty", typeof(decimal));
            poDetailTable.Columns.Add("HargaBeli", typeof(decimal));
            poDetailTable.Columns.Add("Subtotal", typeof(decimal));
            dgvPODetail = BuatGrid("dgvPODetail", 100);
            dgvPODetail.Location = new Point(12, 278);
            dgvPODetail.DataSource = poDetailTable;
            dgvPODetail.CellDoubleClick += dgvPODetail_CellDoubleClick;
            if (dgvPODetail.Columns["ID_Item"] != null)
                dgvPODetail.Columns["ID_Item"].Visible = false;

            lblTotalPO = BuatLabel("Total PO: Rp 0", 12, 384);
            lblTotalPO.Font = Theme.Font(11, FontStyle.Bold);
            txtDPPO = BuatTextBox("txtDPPO", 12, 428, 150, "0");
            txtCatatanPO = BuatTextBox("txtCatatanPO", 180, 428, 300, "Catatan (opsional)");

            btnSimpanPO = BuatButton("btnSimpanPO", "Simpan PO", 12, 468, btnSimpanPO_Click);
            btnKirimPO = BuatButton("btnKirimPO", "Kirim", 144, 468, btnKirimPO_Click);
            btnTerimaPO = BuatButton("btnTerimaPO", "Terima Barang", 276, 468, btnTerimaPO_Click);
            btnTerimaPO.Size = new Size(130, 42);
            btnBatalPO = BuatButton("btnBatalPO", "Batalkan", 418, 468, btnBatalPO_Click);
            btnCetakPO = BuatButton("btnCetakPO", "Cetak PO", 550, 468, btnCetakPO_Click);
            btnClearPO = BuatButton("btnClearPO", "Baru", 682, 468, btnClearPO_Click);

            tabPO.Controls.Add(txtCariPO);
            tabPO.Controls.Add(dgvPO);
            tabPO.Controls.Add(BuatLabel("Supplier", 12, 214));
            tabPO.Controls.Add(BuatLabel("Item", 230, 214));
            tabPO.Controls.Add(BuatLabel("Qty", 450, 214));
            tabPO.Controls.Add(BuatLabel("Harga Beli", 530, 214));
            tabPO.Controls.Add(cmbSupplierPO);
            tabPO.Controls.Add(cmbItemPO);
            tabPO.Controls.Add(numQtyPO);
            tabPO.Controls.Add(txtHargaBeliPO);
            tabPO.Controls.Add(btnTambahItemPO);
            tabPO.Controls.Add(dgvPODetail);
            tabPO.Controls.Add(lblTotalPO);
            tabPO.Controls.Add(BuatLabel("DP (Rp)", 12, 408));
            tabPO.Controls.Add(BuatLabel("Catatan", 180, 408));
            tabPO.Controls.Add(txtDPPO);
            tabPO.Controls.Add(txtCatatanPO);
            tabPO.Controls.Add(btnSimpanPO);
            tabPO.Controls.Add(btnKirimPO);
            tabPO.Controls.Add(btnTerimaPO);
            tabPO.Controls.Add(btnBatalPO);
            tabPO.Controls.Add(btnCetakPO);
            tabPO.Controls.Add(btnClearPO);
            PerbaruiTombolPO();
        }

        private void BangunTabHutang()
        {
            txtCariHutang = BuatCari("txtCariHutang", txtCariHutang_TextChanged);
            txtCariHutang.PlaceholderText = "Cari supplier...";
            dgvHutang = BuatGrid("dgvHutang", 380);
            dgvHutang.CellFormatting += dgvHutang_CellFormatting;
            btnBayarHutang = BuatButton("btnBayarHutang", "Catat Pembayaran", 12, 452, btnBayarHutang_Click);
            btnBayarHutang.Size = new Size(180, 42);
            btnDetailHutang = BuatButton("btnDetailHutang", "Detail", 204, 452, btnDetailHutang_Click);

            tabHutang.Controls.Add(txtCariHutang);
            tabHutang.Controls.Add(dgvHutang);
            tabHutang.Controls.Add(btnBayarHutang);
            tabHutang.Controls.Add(btnDetailHutang);
        }

        private void TerapkanTema()
        {
            this.BackColor = Theme.Background;
            this.Font = Theme.Font(10);
            Theme.StyleTabControl(tabControl);
            Theme.StyleGrid(dgvPO);
            Theme.StyleGrid(dgvPODetail);
            Theme.StyleGrid(dgvHutang);
            Theme.StylePrimaryButton(btnSimpanPO);
            Theme.StylePrimaryButton(btnKirimPO);
            Theme.StyleSuccessButton(btnTerimaPO);
            Theme.StyleDangerButton(btnBatalPO);
            Theme.StyleGhostButton(btnCetakPO);
            Theme.StyleGhostButton(btnClearPO);
            Theme.StylePrimaryButton(btnTambahItemPO);
            Theme.StylePrimaryButton(btnBayarHutang);
            Theme.StyleGhostButton(btnDetailHutang);
            Theme.StyleComboBox(cmbSupplierPO);
            Theme.StyleComboBox(cmbItemPO);
            Theme.StyleNumericUpDown(numQtyPO);
            Theme.StyleTextBox(txtHargaBeliPO);
            Theme.StyleTextBox(txtDPPO);
            Theme.StyleTextBox(txtCatatanPO);
            Theme.StyleTextBox(txtCariPO);
            Theme.StyleTextBox(txtCariHutang);
            lblTotalPO.ForeColor = Theme.Text;
        }

        // ==================== HELPER ====================
        private static bool TryParseDecimal(string teks, out decimal nilai)
        {
            teks = (teks ?? "").Trim();
            if (decimal.TryParse(teks, NumberStyles.Number, CultureInfo.CurrentCulture, out nilai))
                return true;
            return decimal.TryParse(teks, NumberStyles.Number, CultureInfo.InvariantCulture, out nilai);
        }

        private static bool MelacakStok(string tipe)
        {
            return tipe != "Jasa" && tipe != "NonInventori";
        }

        private static string EscapeFilter(string kata)
        {
            kata = kata.Replace("'", "''");
            kata = kata.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
            return kata;
        }

        // ==================== PO: MUAT DATA ====================
        private void LoadSupplierCombo()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Supplier, Nama_Supplier FROM Supplier ORDER BY Nama_Supplier", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        cmbSupplierPO.DisplayMember = "Nama_Supplier";
                        cmbSupplierPO.ValueMember = "ID_Supplier";
                        cmbSupplierPO.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat supplier:\n" + ex.Message,
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
                        "SELECT ID_Item, Nama, HargaBeli FROM Item ORDER BY Nama", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        itemPOTable = new DataTable();
                        adapter.Fill(itemPOTable);
                        cmbItemPO.DisplayMember = "Nama";
                        cmbItemPO.ValueMember = "ID_Item";
                        cmbItemPO.DataSource = itemPOTable;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat item:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadPO()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT po.ID_PO, po.NoPO, s.Nama_Supplier AS Supplier, " +
                        "po.Tanggal, po.Total, po.Status FROM PurchaseOrder po " +
                        "LEFT JOIN Supplier s ON s.ID_Supplier = po.ID_Supplier " +
                        "ORDER BY po.ID_PO DESC", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        poTable = new DataTable();
                        adapter.Fill(poTable);
                        dgvPO.DataSource = poTable;
                        if (dgvPO.Columns["ID_PO"] != null)
                            dgvPO.Columns["ID_PO"].Visible = false;
                        if (dgvPO.Columns["Total"] != null)
                            dgvPO.Columns["Total"].DefaultCellStyle.Format = "N0";
                        TerapkanFilterPO();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat PO:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TerapkanFilterPO()
        {
            if (poTable == null) return;
            string kata = EscapeFilter(txtCariPO.Text.Trim());
            poTable.DefaultView.RowFilter = kata.Length == 0
                ? ""
                : "NoPO LIKE '%" + kata + "%' OR Supplier LIKE '%" + kata + "%'";
        }

        private void txtCariPO_TextChanged(object sender, EventArgs e)
        {
            TerapkanFilterPO();
        }

        private void cmbItemPO_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbItemPO.SelectedValue == null || itemPOTable == null) return;
            foreach (DataRow baris in itemPOTable.Rows)
            {
                if (Convert.ToInt32(baris["ID_Item"]) == Convert.ToInt32(cmbItemPO.SelectedValue))
                {
                    txtHargaBeliPO.Text = Convert.ToDecimal(baris["HargaBeli"]).ToString("N0");
                    return;
                }
            }
        }

        private decimal HitungTotalPO()
        {
            decimal total = 0;
            foreach (DataRow baris in poDetailTable.Rows)
                total += Convert.ToDecimal(baris["Subtotal"]);
            lblTotalPO.Text = "Total PO: Rp " + total.ToString("N0");
            return total;
        }

        // ==================== PO: FORM DETAIL ====================
        private void btnTambahItemPO_Click(object sender, EventArgs e)
        {
            if (!bolehEditPO)
            {
                MessageBox.Show("PO ini sudah tidak bisa diubah (status " + statusPODipilih + ").");
                return;
            }
            if (cmbItemPO.SelectedValue == null)
            {
                MessageBox.Show("Pilih item dulu!");
                return;
            }
            decimal qty = numQtyPO.Value;
            if (qty <= 0)
            {
                MessageBox.Show("Qty minimal 1!");
                return;
            }
            if (!TryParseDecimal(txtHargaBeliPO.Text, out decimal hargaBeli) || hargaBeli <= 0)
            {
                MessageBox.Show("Harga beli harus angka lebih dari 0!");
                return;
            }
            int idItem = Convert.ToInt32(cmbItemPO.SelectedValue);
            string nama = Convert.ToString(cmbItemPO.Text);
            DataRow ada = null;
            foreach (DataRow baris in poDetailTable.Rows)
            {
                if (Convert.ToInt32(baris["ID_Item"]) == idItem) { ada = baris; break; }
            }
            if (ada != null)
            {
                ada["Qty"] = Convert.ToDecimal(ada["Qty"]) + qty;
                ada["HargaBeli"] = hargaBeli;
                ada["Subtotal"] = Convert.ToDecimal(ada["Qty"]) * hargaBeli;
            }
            else
            {
                DataRow baris = poDetailTable.NewRow();
                baris["ID_Item"] = idItem;
                baris["Nama"] = nama;
                baris["Qty"] = qty;
                baris["HargaBeli"] = hargaBeli;
                baris["Subtotal"] = qty * hargaBeli;
                poDetailTable.Rows.Add(baris);
            }
            HitungTotalPO();
            numQtyPO.Value = 1;
        }

        private void dgvPODetail_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || !bolehEditPO) return;
            if (MessageBox.Show("Hapus baris ini dari PO?", "Konfirmasi",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                poDetailTable.Rows.RemoveAt(e.RowIndex);
                HitungTotalPO();
            }
        }

        private void dgvPO_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = dgvPO.Rows[e.RowIndex];
            int id = Convert.ToInt32(row.Cells["ID_PO"].Value);
            MuatPOkeForm(id);
        }

        // Muat satu PO (header + detail) ke form untuk dilihat / diubah.
        private void MuatPOkeForm(int idPO)
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Supplier, Total, DP, Catatan, Status " +
                        "FROM PurchaseOrder WHERE ID_PO = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idPO);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read()) return;
                            idPODipilih = idPO;
                            statusPODipilih = Convert.ToString(reader["Status"]);
                            cmbSupplierPO.SelectedValue = Convert.ToInt32(reader["ID_Supplier"]);
                            txtDPPO.Text = Convert.ToDecimal(reader["DP"]).ToString("N0");
                            txtCatatanPO.Text = Convert.ToString(reader["Catatan"]);
                        }
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT d.ID_Item, i.Nama, d.Qty, d.HargaBeli, d.Subtotal " +
                        "FROM PODetail d INNER JOIN Item i ON i.ID_Item = d.ID_Item " +
                        "WHERE d.ID_PO = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idPO);
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        poDetailTable.Clear();
                        adapter.Fill(poDetailTable);
                    }
                }
                HitungTotalPO();
                AturEditPO(statusPODipilih == "Draft");
                PerbaruiTombolPO();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat PO:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AturEditPO(bool boleh)
        {
            bolehEditPO = boleh;
            cmbSupplierPO.Enabled = boleh;
            cmbItemPO.Enabled = boleh;
            numQtyPO.Enabled = boleh;
            txtHargaBeliPO.Enabled = boleh;
            btnTambahItemPO.Enabled = boleh;
            txtDPPO.Enabled = boleh;
            txtCatatanPO.Enabled = boleh;
        }

        private void PerbaruiTombolPO()
        {
            if (idPODipilih == null)
            {
                btnSimpanPO.Enabled = true;
                btnKirimPO.Enabled = false;
                btnTerimaPO.Enabled = false;
                btnBatalPO.Enabled = false;
                btnCetakPO.Enabled = false;
                return;
            }
            switch (statusPODipilih)
            {
                case "Draft":
                    btnSimpanPO.Enabled = true;
                    btnKirimPO.Enabled = true;
                    btnTerimaPO.Enabled = false;
                    btnBatalPO.Enabled = true;
                    btnCetakPO.Enabled = true;
                    break;
                case "Dikirim":
                    btnSimpanPO.Enabled = false;
                    btnKirimPO.Enabled = false;
                    btnTerimaPO.Enabled = true;
                    btnBatalPO.Enabled = false;
                    btnCetakPO.Enabled = true;
                    break;
                default: // Diterima / Dibatalkan
                    btnSimpanPO.Enabled = false;
                    btnKirimPO.Enabled = false;
                    btnTerimaPO.Enabled = false;
                    btnBatalPO.Enabled = false;
                    btnCetakPO.Enabled = true;
                    break;
            }
        }

        private void ResetFormPO()
        {
            idPODipilih = null;
            statusPODipilih = null;
            poDetailTable.Clear();
            txtDPPO.Text = "";
            txtCatatanPO.Text = "";
            HitungTotalPO();
            AturEditPO(true);
            PerbaruiTombolPO();
        }

        private void btnClearPO_Click(object sender, EventArgs e)
        {
            ResetFormPO();
        }

        // ==================== PO: SIMPAN / STATUS ====================
        private string BuatNoPO()
        {
            string tahun = DateTime.Now.ToString("yyyy");
            long jumlah = 0;
            using (var conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT COUNT(*) FROM PurchaseOrder WHERE NoPO LIKE @Pola", conn))
                {
                    cmd.Parameters.AddWithValue("@Pola", "PO-" + tahun + "-%");
                    jumlah = Convert.ToInt64(cmd.ExecuteScalar());
                }
            }
            return "PO-" + tahun + "-" + (jumlah + 1).ToString("D4");
        }

        private void btnSimpanPO_Click(object sender, EventArgs e)
        {
            if (!bolehEditPO)
            {
                MessageBox.Show("PO ini sudah tidak bisa diubah (status " + statusPODipilih + ").");
                return;
            }
            if (cmbSupplierPO.SelectedValue == null)
            {
                MessageBox.Show("Pilih supplier dulu!");
                return;
            }
            if (poDetailTable.Rows.Count == 0)
            {
                MessageBox.Show("Tambahkan minimal 1 item dulu!");
                return;
            }
            decimal total = HitungTotalPO();
            decimal dp = 0;
            if (txtDPPO.Text.Trim().Length > 0)
            {
                if (!TryParseDecimal(txtDPPO.Text, out dp) || dp < 0)
                {
                    MessageBox.Show("DP harus angka 0 atau lebih!");
                    return;
                }
            }
            if (dp > total)
            {
                MessageBox.Show("DP tidak boleh melebihi total PO!");
                return;
            }
            int idSupplier = Convert.ToInt32(cmbSupplierPO.SelectedValue);
            string catatan = txtCatatanPO.Text.Trim();
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        if (idPODipilih == null)
                        {
                            using (var cmd = new SQLiteCommand(
                                "INSERT INTO PurchaseOrder (NoPO, ID_Supplier, Tanggal, " +
                                "Status, Total, DP, Catatan) " +
                                "VALUES (@NoPO, @ID_Supplier, @Tanggal, 'Draft', " +
                                "@Total, @DP, @Catatan)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@NoPO", BuatNoPO());
                                cmd.Parameters.AddWithValue("@ID_Supplier", idSupplier);
                                cmd.Parameters.AddWithValue("@Tanggal", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                cmd.Parameters.AddWithValue("@Total", total);
                                cmd.Parameters.AddWithValue("@DP", dp);
                                cmd.Parameters.AddWithValue("@Catatan", catatan);
                                cmd.ExecuteNonQuery();
                            }
                            using (var cmd = new SQLiteCommand("SELECT last_insert_rowid()", conn, trans))
                            {
                                idPODipilih = (int)(long)cmd.ExecuteScalar();
                            }
                        }
                        else
                        {
                            // Guard: hanya Draft yang boleh diubah. Kalau
                            // status sudah berubah (mis. Dikirim), batalkan.
                            using (var cmd = new SQLiteCommand(
                                "UPDATE PurchaseOrder SET ID_Supplier = @ID_Supplier, " +
                                "Total = @Total, DP = @DP, Catatan = @Catatan " +
                                "WHERE ID_PO = @ID AND Status = 'Draft'", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID_Supplier", idSupplier);
                                cmd.Parameters.AddWithValue("@Total", total);
                                cmd.Parameters.AddWithValue("@DP", dp);
                                cmd.Parameters.AddWithValue("@Catatan", catatan);
                                cmd.Parameters.AddWithValue("@ID", idPODipilih.Value);
                                if (cmd.ExecuteNonQuery() == 0)
                                {
                                    trans.Rollback();
                                    MessageBox.Show("PO sudah tidak Draft — tidak bisa diubah.");
                                    return;
                                }
                            }
                            using (var cmd = new SQLiteCommand(
                                "DELETE FROM PODetail WHERE ID_PO = @ID", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID", idPODipilih.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        foreach (DataRow baris in poDetailTable.Rows)
                        {
                            using (var cmd = new SQLiteCommand(
                                "INSERT INTO PODetail (ID_PO, ID_Item, Qty, HargaBeli, Subtotal) " +
                                "VALUES (@ID_PO, @ID_Item, @Qty, @HargaBeli, @Subtotal)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID_PO", idPODipilih.Value);
                                cmd.Parameters.AddWithValue("@ID_Item", baris["ID_Item"]);
                                cmd.Parameters.AddWithValue("@Qty", baris["Qty"]);
                                cmd.Parameters.AddWithValue("@HargaBeli", baris["HargaBeli"]);
                                cmd.Parameters.AddWithValue("@Subtotal", baris["Subtotal"]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        trans.Commit();
                    }
                }
                int idSimpan = idPODipilih.Value;
                LoadPO();
                MuatPOkeForm(idSimpan);
                MessageBox.Show("PO tersimpan (Draft).");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menyimpan PO:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnKirimPO_Click(object sender, EventArgs e)
        {
            if (idPODipilih == null) return;
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "UPDATE PurchaseOrder SET Status = 'Dikirim' " +
                        "WHERE ID_PO = @ID AND Status = 'Draft'", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idPODipilih.Value);
                        if (cmd.ExecuteNonQuery() == 0)
                        {
                            MessageBox.Show("PO sudah tidak Draft — tidak bisa dikirim.");
                            return;
                        }
                    }
                }
                LoadPO();
                ResetFormPO();
                MessageBox.Show("PO dikirim ke supplier.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mengirim PO:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Terima barang: SATU transaksi — kunci status (hanya dari Dikirim),
        // tambah stok + StokMovement 'Masuk' per item yang melacak stok,
        // lalu buat Hutang kalau Total > DP (jatuh tempo +30 hari).
        private void btnTerimaPO_Click(object sender, EventArgs e)
        {
            if (idPODipilih == null) return;
            try
            {
                string noPO = "";
                decimal total = 0;
                decimal dp = 0;
                int idSupplier = 0;
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT NoPO, Total, DP, ID_Supplier FROM PurchaseOrder " +
                        "WHERE ID_PO = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idPODipilih.Value);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show("PO tidak ditemukan.");
                                return;
                            }
                            noPO = Convert.ToString(reader["NoPO"]);
                            total = Convert.ToDecimal(reader["Total"]);
                            dp = Convert.ToDecimal(reader["DP"]);
                            idSupplier = Convert.ToInt32(reader["ID_Supplier"]);
                        }
                    }
                    string tanggalTerima = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string jatuhTempo = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd");
                    using (var trans = conn.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand(
                            "UPDATE PurchaseOrder SET Status = 'Diterima' " +
                            "WHERE ID_PO = @ID AND Status = 'Dikirim'", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", idPODipilih.Value);
                            if (cmd.ExecuteNonQuery() == 0)
                            {
                                trans.Rollback();
                                MessageBox.Show("PO sudah tidak dalam status Dikirim — " +
                                    "mungkin sudah diterima.");
                                return;
                            }
                        }
                        DataTable detail = new DataTable();
                        using (var cmd = new SQLiteCommand(
                            "SELECT d.ID_Item, d.Qty, d.HargaBeli, i.Tipe FROM PODetail d " +
                            "INNER JOIN Item i ON i.ID_Item = d.ID_Item " +
                            "WHERE d.ID_PO = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", idPODipilih.Value);
                            SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                            adapter.Fill(detail);
                        }
                        foreach (DataRow baris in detail.Rows)
                        {
                            if (!MelacakStok(Convert.ToString(baris["Tipe"])))
                                continue;
                            int idItem = Convert.ToInt32(baris["ID_Item"]);
                            decimal qty = Convert.ToDecimal(baris["Qty"]);
                            decimal hargaBeli = Convert.ToDecimal(baris["HargaBeli"]);
                            // Fase 3: stok naik di Item + StokLokasi
                            // (gudang utama) + batch baru per detail PO
                            // (HargaBeli dari PO — dasar HPP FIFO/LIFO).
                            decimal stokSesudah = StokHelper.NaikkanStok(
                                conn, trans, idItem, StokHelper.LokasiUtama, qty);
                            StokHelper.BuatBatch(conn, trans, idItem,
                                StokHelper.LokasiUtama, tanggalTerima, qty,
                                hargaBeli, "PO", idPODipilih.Value);
                            using (var cmd = new SQLiteCommand(
                                "INSERT INTO StokMovement (ID_Item, Tanggal, Jenis, Qty, " +
                                "StokSesudah, RefID, Keterangan, ID_Lokasi) " +
                                "VALUES (@ID_Item, @Tanggal, 'Masuk', @Qty, " +
                                "@StokSesudah, @RefID, 'Terima PO', @Lokasi)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID_Item", idItem);
                                cmd.Parameters.AddWithValue("@Tanggal", tanggalTerima);
                                cmd.Parameters.AddWithValue("@Qty", qty);
                                cmd.Parameters.AddWithValue("@StokSesudah", stokSesudah);
                                cmd.Parameters.AddWithValue("@RefID", "PO-" + noPO);
                                cmd.Parameters.AddWithValue("@Lokasi", StokHelper.LokasiUtama);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        if (total > dp)
                        {
                            using (var cmd = new SQLiteCommand(
                                "INSERT INTO Hutang (RefType, RefID, ID_Supplier, Tanggal, " +
                                "JatuhTempo, Total, Sisa, Status) " +
                                "VALUES ('PO', @RefID, @ID_Supplier, @Tanggal, " +
                                "@JatuhTempo, @Total, @Sisa, 'Belum Lunas')", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@RefID", idPODipilih.Value);
                                cmd.Parameters.AddWithValue("@ID_Supplier", idSupplier);
                                cmd.Parameters.AddWithValue("@Tanggal", tanggalTerima);
                                cmd.Parameters.AddWithValue("@JatuhTempo", jatuhTempo);
                                cmd.Parameters.AddWithValue("@Total", total);
                                cmd.Parameters.AddWithValue("@Sisa", total - dp);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        // Fase 4: jurnal terima PO (dalam transaksi yang
                        // sama — kalau jurnal tidak seimbang, terima barang
                        // ikut di-rollback). Debit Persediaan [Total];
                        // Kredit Kas [DP]; Kredit Hutang Usaha [Total - DP];
                        // kalau DP > Total, kelebihan masuk Uang Muka
                        // Pembelian (lihat JurnalHelper).
                        JurnalHelper.TulisJurnalPembelian(conn, trans,
                            tanggalTerima, noPO, idPODipilih.Value, total, dp);
                        trans.Commit();
                    }
                }
                LoadPO();
                ResetFormPO();
                MessageBox.Show("Barang diterima, stok bertambah." +
                    (total > dp
                        ? " Hutang Rp " + (total - dp).ToString("N0") + " tercatat (jatuh tempo +30 hari)."
                        : " Lunas — DP menutupi total."));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menerima barang:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnBatalPO_Click(object sender, EventArgs e)
        {
            if (idPODipilih == null) return;
            if (MessageBox.Show("Batalkan PO ini?", "Konfirmasi",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "UPDATE PurchaseOrder SET Status = 'Dibatalkan' " +
                        "WHERE ID_PO = @ID AND Status = 'Draft'", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idPODipilih.Value);
                        if (cmd.ExecuteNonQuery() == 0)
                        {
                            MessageBox.Show("PO sudah tidak Draft — tidak bisa dibatalkan.");
                            return;
                        }
                    }
                }
                LoadPO();
                ResetFormPO();
                MessageBox.Show("PO dibatalkan.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal membatalkan PO:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==================== PO: CETAK ====================
        private void btnCetakPO_Click(object sender, EventArgs e)
        {
            if (idPODipilih == null)
            {
                MessageBox.Show("Pilih PO dulu yang mau dicetak!");
                return;
            }
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT po.NoPO, po.Tanggal, po.Total, po.DP, s.Nama_Supplier " +
                        "FROM PurchaseOrder po LEFT JOIN Supplier s " +
                        "ON s.ID_Supplier = po.ID_Supplier WHERE po.ID_PO = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idPODipilih.Value);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read()) return;
                            printNoPO = Convert.ToString(reader["NoPO"]);
                            printTanggal = Convert.ToString(reader["Tanggal"]);
                            printTotal = Convert.ToDecimal(reader["Total"]);
                            printDP = Convert.ToDecimal(reader["DP"]);
                            printSupplier = Convert.ToString(reader["Nama_Supplier"]);
                        }
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT i.Nama, d.Qty, d.HargaBeli, d.Subtotal " +
                        "FROM PODetail d INNER JOIN Item i ON i.ID_Item = d.ID_Item " +
                        "WHERE d.ID_PO = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idPODipilih.Value);
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        printDetail = new DataTable();
                        adapter.Fill(printDetail);
                    }
                }
                printIndex = 0;
                docPO = new PrintDocument();
                docPO.DocumentName = "PO " + printNoPO;
                docPO.PrintPage += DocPO_PrintPage;
                PrintPreviewDialog preview = new PrintPreviewDialog();
                preview.Document = docPO;
                preview.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mencetak PO:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DocPO_PrintPage(object sender, PrintPageEventArgs e)
        {
            Font judul = new Font("Segoe UI", 14, FontStyle.Bold);
            Font biasa = new Font("Segoe UI", 10);
            Font tebal = new Font("Segoe UI", 10, FontStyle.Bold);
            float x = e.MarginBounds.Left;
            float y = e.MarginBounds.Top;
            e.Graphics.DrawString("PURCHASE ORDER", judul, Brushes.Black, x, y);
            y += 30;
            e.Graphics.DrawString("No PO: " + printNoPO, biasa, Brushes.Black, x, y);
            y += 20;
            e.Graphics.DrawString("Tanggal: " + printTanggal, biasa, Brushes.Black, x, y);
            y += 20;
            e.Graphics.DrawString("Supplier: " + printSupplier, biasa, Brushes.Black, x, y);
            y += 30;
            e.Graphics.DrawString("Item", tebal, Brushes.Black, x, y);
            e.Graphics.DrawString("Qty", tebal, Brushes.Black, x + 300, y);
            e.Graphics.DrawString("Harga Beli", tebal, Brushes.Black, x + 380, y);
            e.Graphics.DrawString("Subtotal", tebal, Brushes.Black, x + 500, y);
            y += 20;
            while (printIndex < printDetail.Rows.Count)
            {
                DataRow baris = printDetail.Rows[printIndex];
                e.Graphics.DrawString(Convert.ToString(baris["Nama"]), biasa, Brushes.Black, x, y);
                e.Graphics.DrawString(Convert.ToDecimal(baris["Qty"]).ToString("N0"), biasa, Brushes.Black, x + 300, y);
                e.Graphics.DrawString(Convert.ToDecimal(baris["HargaBeli"]).ToString("N0"), biasa, Brushes.Black, x + 380, y);
                e.Graphics.DrawString(Convert.ToDecimal(baris["Subtotal"]).ToString("N0"), biasa, Brushes.Black, x + 500, y);
                y += 20;
                printIndex++;
                if (y > e.MarginBounds.Bottom - 80)
                {
                    e.HasMorePages = true;
                    return;
                }
            }
            y += 10;
            e.Graphics.DrawString("Total: Rp " + printTotal.ToString("N0"), tebal, Brushes.Black, x, y);
            y += 20;
            e.Graphics.DrawString("DP: Rp " + printDP.ToString("N0"), biasa, Brushes.Black, x, y);
            y += 20;
            e.Graphics.DrawString("Sisa (hutang): Rp " + Math.Max(0, printTotal - printDP).ToString("N0"),
                biasa, Brushes.Black, x, y);
            e.HasMorePages = false;
        }

        // ==================== HUTANG ====================
        private void LoadHutang()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT h.ID_Hutang, h.RefType, h.RefID, s.Nama_Supplier AS Supplier, " +
                        "h.Tanggal, h.JatuhTempo, h.Total, h.Sisa, h.Status FROM Hutang h " +
                        "LEFT JOIN Supplier s ON s.ID_Supplier = h.ID_Supplier " +
                        "ORDER BY h.ID_Hutang DESC", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        hutangTable = new DataTable();
                        adapter.Fill(hutangTable);
                        dgvHutang.DataSource = hutangTable;
                        if (dgvHutang.Columns["ID_Hutang"] != null)
                            dgvHutang.Columns["ID_Hutang"].Visible = false;
                        if (dgvHutang.Columns["RefType"] != null)
                            dgvHutang.Columns["RefType"].Visible = false;
                        if (dgvHutang.Columns["RefID"] != null)
                            dgvHutang.Columns["RefID"].Visible = false;
                        if (dgvHutang.Columns["Total"] != null)
                            dgvHutang.Columns["Total"].DefaultCellStyle.Format = "N0";
                        if (dgvHutang.Columns["Sisa"] != null)
                            dgvHutang.Columns["Sisa"].DefaultCellStyle.Format = "N0";
                        TerapkanFilterHutang();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat hutang:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TerapkanFilterHutang()
        {
            if (hutangTable == null) return;
            string kata = EscapeFilter(txtCariHutang.Text.Trim());
            hutangTable.DefaultView.RowFilter = kata.Length == 0
                ? ""
                : "Supplier LIKE '%" + kata + "%'";
        }

        private void txtCariHutang_TextChanged(object sender, EventArgs e)
        {
            TerapkanFilterHutang();
        }

        // Baris jatuh tempo merah (teks) kalau sudah lewat dan belum lunas.
        private void dgvHutang_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvHutang.Columns[e.ColumnIndex].Name != "JatuhTempo") return;
            DataGridViewRow row = dgvHutang.Rows[e.RowIndex];
            if (Convert.ToString(row.Cells["Status"].Value) != "Belum Lunas") return;
            DateTime jatuh;
            if (DateTime.TryParse(Convert.ToString(row.Cells["JatuhTempo"].Value), out jatuh)
                && jatuh.Date < DateTime.Now.Date)
            {
                e.CellStyle.ForeColor = Theme.Danger;
                e.CellStyle.Font = Theme.Font(10, FontStyle.Bold);
            }
        }

        private void btnBayarHutang_Click(object sender, EventArgs e)
        {
            if (dgvHutang.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih hutang dulu yang mau dibayar!");
                return;
            }
            DataGridViewRow row = dgvHutang.SelectedRows[0];
            int idHutang = Convert.ToInt32(row.Cells["ID_Hutang"].Value);
            decimal sisa = Convert.ToDecimal(row.Cells["Sisa"].Value);
            decimal total = Convert.ToDecimal(row.Cells["Total"].Value);
            string supplier = Convert.ToString(row.Cells["Supplier"].Value);
            string status = Convert.ToString(row.Cells["Status"].Value);
            if (status == "Lunas")
            {
                MessageBox.Show("Hutang ini sudah lunas.");
                return;
            }
            string konteks = supplier;
            string refType = Convert.ToString(row.Cells["RefType"].Value);
            int refID = row.Cells["RefID"].Value == DBNull.Value
                ? 0 : Convert.ToInt32(row.Cells["RefID"].Value);
            if (refType == "PO" && refID > 0)
            {
                try
                {
                    using (var conn = ConnectionManager.GetConnection())
                    {
                        conn.Open();
                        using (var cmd = new SQLiteCommand(
                            "SELECT NoPO FROM PurchaseOrder WHERE ID_PO = @ID", conn))
                        {
                            cmd.Parameters.AddWithValue("@ID", refID);
                            object hasil = cmd.ExecuteScalar();
                            if (hasil != null) konteks += " — " + Convert.ToString(hasil);
                        }
                    }
                }
                catch
                {
                    // Konteks gagal dimuat: tetap lanjut, bukan halangan bayar.
                }
            }
            FormCatatPembayaran form = new FormCatatPembayaran();
            form.Judul = "Catat Pembayaran Hutang";
            form.Konteks = konteks;
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
                        // Baca ulang sisa di dalam transaksi — kalau sudah
                        // berubah (mis. dibayar dari tempat lain), tolak.
                        decimal sisaSekarang;
                        using (var cmd = new SQLiteCommand(
                            "SELECT Sisa FROM Hutang WHERE ID_Hutang = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", idHutang);
                            sisaSekarang = Convert.ToDecimal(cmd.ExecuteScalar());
                        }
                        if (form.Jumlah > sisaSekarang)
                        {
                            trans.Rollback();
                            MessageBox.Show("Jumlah melebihi sisa hutang Rp " +
                                sisaSekarang.ToString("N0") + ".");
                            return;
                        }
                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO HutangBayar (ID_Hutang, Tanggal, Jumlah, Keterangan) " +
                            "VALUES (@ID_Hutang, @Tanggal, @Jumlah, @Keterangan)", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID_Hutang", idHutang);
                            cmd.Parameters.AddWithValue("@Tanggal", form.Tanggal.ToString("yyyy-MM-dd"));
                            cmd.Parameters.AddWithValue("@Jumlah", form.Jumlah);
                            cmd.Parameters.AddWithValue("@Keterangan", form.Keterangan);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand(
                            "UPDATE Hutang SET Sisa = Sisa - @Jumlah, " +
                            "Status = CASE WHEN Sisa - @Jumlah <= 0 THEN 'Lunas' " +
                            "ELSE 'Belum Lunas' END " +
                            "WHERE ID_Hutang = @ID AND Sisa >= @Jumlah", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Jumlah", form.Jumlah);
                            cmd.Parameters.AddWithValue("@ID", idHutang);
                            if (cmd.ExecuteNonQuery() == 0)
                            {
                                trans.Rollback();
                                MessageBox.Show("Pembayaran gagal — sisa hutang sudah berubah.");
                                return;
                            }
                        }
                        // Fase 4: jurnal bayar hutang (dalam transaksi yang
                        // sama). Hutang titipan konsinyasi (RefType
                        // 'KonsinyasiMasuk') memakai akun 2102 Hutang
                        // Konsinyasi, yang lain 2101 Hutang Usaha.
                        JurnalHelper.TulisJurnalBayarHutang(conn, trans,
                            form.Tanggal.ToString("yyyy-MM-dd HH:mm:ss"),
                            form.Jumlah, refType, idHutang);
                        trans.Commit();
                    }
                }
                LoadHutang();
                MessageBox.Show("Pembayaran Rp " + form.Jumlah.ToString("N0") + " tercatat.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mencatat pembayaran:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDetailHutang_Click(object sender, EventArgs e)
        {
            if (dgvHutang.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih hutang dulu yang mau dilihat detailnya!");
                return;
            }
            DataGridViewRow row = dgvHutang.SelectedRows[0];
            int idHutang = Convert.ToInt32(row.Cells["ID_Hutang"].Value);
            decimal sisa = Convert.ToDecimal(row.Cells["Sisa"].Value);
            decimal total = Convert.ToDecimal(row.Cells["Total"].Value);
            string supplier = Convert.ToString(row.Cells["Supplier"].Value);
            try
            {
                DataTable riwayat = new DataTable();
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT Tanggal, Jumlah, Keterangan FROM HutangBayar " +
                        "WHERE ID_Hutang = @ID ORDER BY Tanggal DESC, ID_HutangBayar DESC", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idHutang);
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        adapter.Fill(riwayat);
                    }
                }
                FormDetailPembayaran form = new FormDetailPembayaran();
                form.Judul = "Detail Pembayaran Hutang";
                form.Konteks = supplier;
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
