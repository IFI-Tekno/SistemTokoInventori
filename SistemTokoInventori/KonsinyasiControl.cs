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
    /// Modul Konsinyasi (Fase 3): titipan masuk dari supplier + titipan
    /// keluar ke agen/toko lain.
    ///
    /// KONSINYASI MASUK: barang titipan supplier BUKAN stok toko — tidak
    /// masuk Item.Stok / StokLokasi, hanya tercatat di detail titipan
    /// (QtyTitipan - QtyTerjual = sisa). "Catat Penjualan Titipan" dalam
    /// SATU transaksi: QtyTerjual naik, Transaksi tunai + Detail (HPP =
    /// qty x HargaTitipan) tercatat, log 'Jual Konsinyasi' ditulis (stok
    /// toko tidak berubah), dan Hutang dibuat sebesar qty x HargaTitipan
    /// (jatuh tempo +30 hari) — hutang HANYA untuk barang yang sudah
    /// benar-benar terjual. Kalau semua detail laku, status jadi 'Selesai'.
    /// KSM Aktif bisa dibatalkan hanya kalau belum ada yang terjual.
    ///
    /// KONSINYASI KELUAR: menitipkan barang toko ke agen. Saat KSK dibuat,
    /// dalam SATU transaksi: stok lokasi 1 + Item.Stok berkurang, batch
    /// dikonsumsi (HPP per unit disimpan di detail), log 'Konsinyasi
    /// Keluar' ditulis. "Catat Penjualan Agen" dalam SATU transaksi:
    /// QtyTerjual naik, Transaksi + Detail (HPP dari HPPUnit) tercatat,
    /// log 'Jual Konsinyasi Keluar' ditulis (stok tidak berubah — sudah
    /// keluar saat dititipkan), dan Piutang dibuat ke agen (jatuh tempo
    /// +14 hari). Barang yang tidak laku bisa "Terima Kembali": stok
    /// naik lagi + batch baru seharga HPPUnit. Kalau semua detail laku
    /// atau kembali, status jadi 'Selesai'.
    ///
    /// UI dibangun dari kode (tanpa Designer) seperti PembelianControl.
    /// </summary>
    public partial class KonsinyasiControl : UserControl
    {
        private Guna2TabControl tabControl;
        private TabPage tabMasuk;
        private TabPage tabKeluar;

        // ----- Konsinyasi Masuk -----
        private Guna2TextBox txtCariKSM;
        private Guna2DataGridView dgvKSM;
        private Guna2ComboBox cmbSupplierKSM;
        private Guna2ComboBox cmbItemKSM;
        private Guna2NumericUpDown numQtyKSM;
        private Guna2TextBox txtHargaTitipanKSM;
        private Guna2TextBox txtHargaJualKSM;
        private Guna2Button btnTambahItemKSM;
        private Guna2DataGridView dgvKSMDetail;
        private Guna2TextBox txtCatatanKSM;
        private Guna2Button btnSimpanKSM;
        private Guna2Button btnBatalKSM;
        private Guna2Button btnClearKSM;
        private Guna2Button btnJualKSM;
        private DataTable ksmTable;
        private DataTable ksmDetailTable;
        private DataTable itemKSMTable;
        // KSM yang sedang dibuka di form (null = KSM baru). KSM yang sudah
        // tersimpan tidak bisa diubah — hanya dibatalkan (kalau belum ada
        // yang terjual) atau dicatat penjualannya.
        private int? idKSMPilihan;
        private string statusKSMPilihan;
        private bool bolehEditKSM = true;

        // ----- Konsinyasi Keluar -----
        private Guna2TextBox txtCariKSK;
        private Guna2DataGridView dgvKSK;
        private Guna2ComboBox cmbPelangganKSK;
        private Guna2ComboBox cmbItemKSK;
        private Guna2NumericUpDown numQtyKSK;
        private Guna2TextBox txtHargaJualKSK;
        private Guna2Button btnTambahItemKSK;
        private Guna2DataGridView dgvKSKDetail;
        private Guna2TextBox txtCatatanKSK;
        private Guna2Button btnSimpanKSK;
        private Guna2Button btnClearKSK;
        private Guna2Button btnJualKSK;
        private Guna2Button btnKembaliKSK;
        private DataTable kskTable;
        private DataTable kskDetailTable;
        private DataTable itemKSKTable;
        // KSK yang sedang dibuka di form (null = KSK baru). KSK yang sudah
        // tersimpan tidak bisa diubah — hanya dicatat penjualan agennya
        // atau diterima kembali barangnya.
        private int? idKSKPilihan;
        private string statusKSKPilihan;

        public KonsinyasiControl()
        {
            InitializeComponent();
            TerapkanTema();
            LoadSupplierCombo();
            LoadPelangganCombo();
            LoadItemComboKSM();
            LoadItemComboKSK();
            LoadKSM();
            LoadKSK();
        }

        /// <summary>Dipanggil FormMain setiap kali tab Konsinyasi dibuka.</summary>
        public void RefreshData()
        {
            LoadSupplierCombo();
            LoadPelangganCombo();
            LoadItemComboKSM();
            LoadItemComboKSK();
            LoadKSM();
            LoadKSK();
        }

        // ==================== PEMBANGUN UI ====================
        private void InitializeComponent()
        {
            this.tabControl = new Guna.UI2.WinForms.Guna2TabControl();
            this.tabMasuk = new System.Windows.Forms.TabPage();
            this.tabKeluar = new System.Windows.Forms.TabPage();
            this.tabControl.Controls.Add(this.tabMasuk);
            this.tabControl.Controls.Add(this.tabKeluar);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Name = "tabControl";
            this.tabMasuk.Text = "Konsinyasi Masuk";
            this.tabKeluar.Text = "Konsinyasi Keluar";

            BangunTabMasuk();
            BangunTabKeluar();

            this.Controls.Add(this.tabControl);
            this.Name = "KonsinyasiControl";
            this.Size = new System.Drawing.Size(900, 620);
            var header = Theme.BuildHeader("Konsinyasi", "Titipan barang dari supplier dan ke agen");
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

        private void BangunTabMasuk()
        {
            txtCariKSM = BuatCari("txtCariKSM", txtCariKSM_TextChanged);
            txtCariKSM.PlaceholderText = "Cari No KSM / supplier...";
            dgvKSM = BuatGrid("dgvKSM", 130);
            dgvKSM.CellClick += dgvKSM_CellClick;

            cmbSupplierKSM = new Guna2ComboBox();
            cmbSupplierKSM.Name = "cmbSupplierKSM";
            cmbSupplierKSM.Font = Theme.Font(10);
            cmbSupplierKSM.Location = new Point(12, 216);
            cmbSupplierKSM.Size = new Size(200, 30);
            cmbSupplierKSM.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbItemKSM = new Guna2ComboBox();
            cmbItemKSM.Name = "cmbItemKSM";
            cmbItemKSM.Font = Theme.Font(10);
            cmbItemKSM.Location = new Point(220, 216);
            cmbItemKSM.Size = new Size(200, 30);
            cmbItemKSM.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbItemKSM.SelectedIndexChanged += cmbItemKSM_SelectedIndexChanged;
            numQtyKSM = new Guna2NumericUpDown();
            numQtyKSM.Name = "numQtyKSM";
            numQtyKSM.Font = Theme.Font(10);
            numQtyKSM.Location = new Point(428, 216);
            numQtyKSM.Size = new Size(60, 30);
            numQtyKSM.Minimum = 1;
            numQtyKSM.Maximum = 99999;
            numQtyKSM.Value = 1;
            txtHargaTitipanKSM = BuatTextBox("txtHargaTitipanKSM", 496, 216, 100, "0");
            txtHargaJualKSM = BuatTextBox("txtHargaJualKSM", 604, 216, 100, "0");
            btnTambahItemKSM = BuatButton("btnTambahItemKSM", "Tambah Item", 712, 216, btnTambahItemKSM_Click);
            btnTambahItemKSM.Size = new Size(120, 36);

            ksmDetailTable = new DataTable();
            ksmDetailTable.Columns.Add("ID_KSMDetail", typeof(int));
            ksmDetailTable.Columns.Add("ID_Item", typeof(int));
            ksmDetailTable.Columns.Add("Nama", typeof(string));
            ksmDetailTable.Columns.Add("QtyTitipan", typeof(decimal));
            ksmDetailTable.Columns.Add("QtyTerjual", typeof(decimal));
            ksmDetailTable.Columns.Add("Sisa", typeof(decimal));
            ksmDetailTable.Columns.Add("HargaTitipan", typeof(decimal));
            ksmDetailTable.Columns.Add("HargaJual", typeof(decimal));
            dgvKSMDetail = BuatGrid("dgvKSMDetail", 100);
            dgvKSMDetail.Location = new Point(12, 256);
            dgvKSMDetail.DataSource = ksmDetailTable;
            dgvKSMDetail.CellDoubleClick += dgvKSMDetail_CellDoubleClick;
            if (dgvKSMDetail.Columns["ID_KSMDetail"] != null)
                dgvKSMDetail.Columns["ID_KSMDetail"].Visible = false;
            if (dgvKSMDetail.Columns["ID_Item"] != null)
                dgvKSMDetail.Columns["ID_Item"].Visible = false;
            if (dgvKSMDetail.Columns["QtyTitipan"] != null)
                dgvKSMDetail.Columns["QtyTitipan"].DefaultCellStyle.Format = "N0";
            if (dgvKSMDetail.Columns["QtyTerjual"] != null)
                dgvKSMDetail.Columns["QtyTerjual"].DefaultCellStyle.Format = "N0";
            if (dgvKSMDetail.Columns["Sisa"] != null)
                dgvKSMDetail.Columns["Sisa"].DefaultCellStyle.Format = "N0";
            if (dgvKSMDetail.Columns["HargaTitipan"] != null)
                dgvKSMDetail.Columns["HargaTitipan"].DefaultCellStyle.Format = "N0";
            if (dgvKSMDetail.Columns["HargaJual"] != null)
                dgvKSMDetail.Columns["HargaJual"].DefaultCellStyle.Format = "N0";

            txtCatatanKSM = BuatTextBox("txtCatatanKSM", 12, 386, 300, "Catatan (opsional)");

            btnSimpanKSM = BuatButton("btnSimpanKSM", "Simpan KSM", 12, 436, btnSimpanKSM_Click);
            btnBatalKSM = BuatButton("btnBatalKSM", "Batalkan", 144, 436, btnBatalKSM_Click);
            btnClearKSM = BuatButton("btnClearKSM", "Baru", 276, 436, btnClearKSM_Click);
            btnJualKSM = BuatButton("btnJualKSM", "Catat Penjualan", 408, 436, btnJualKSM_Click);
            btnJualKSM.Size = new Size(180, 42);

            tabMasuk.Controls.Add(txtCariKSM);
            tabMasuk.Controls.Add(dgvKSM);
            tabMasuk.Controls.Add(BuatLabel("Supplier", 12, 196));
            tabMasuk.Controls.Add(BuatLabel("Item", 220, 196));
            tabMasuk.Controls.Add(BuatLabel("Qty", 428, 196));
            tabMasuk.Controls.Add(BuatLabel("Harga Titipan", 496, 196));
            tabMasuk.Controls.Add(BuatLabel("Harga Jual", 604, 196));
            tabMasuk.Controls.Add(cmbSupplierKSM);
            tabMasuk.Controls.Add(cmbItemKSM);
            tabMasuk.Controls.Add(numQtyKSM);
            tabMasuk.Controls.Add(txtHargaTitipanKSM);
            tabMasuk.Controls.Add(txtHargaJualKSM);
            tabMasuk.Controls.Add(btnTambahItemKSM);
            tabMasuk.Controls.Add(dgvKSMDetail);
            tabMasuk.Controls.Add(BuatLabel("Catatan", 12, 366));
            tabMasuk.Controls.Add(txtCatatanKSM);
            tabMasuk.Controls.Add(btnSimpanKSM);
            tabMasuk.Controls.Add(btnBatalKSM);
            tabMasuk.Controls.Add(btnClearKSM);
            tabMasuk.Controls.Add(btnJualKSM);
            PerbaruiTombolKSM();
        }

        private void BangunTabKeluar()
        {
            txtCariKSK = BuatCari("txtCariKSK", txtCariKSK_TextChanged);
            txtCariKSK.PlaceholderText = "Cari No KSK / agen...";
            dgvKSK = BuatGrid("dgvKSK", 130);
            dgvKSK.CellClick += dgvKSK_CellClick;

            cmbPelangganKSK = new Guna2ComboBox();
            cmbPelangganKSK.Name = "cmbPelangganKSK";
            cmbPelangganKSK.Font = Theme.Font(10);
            cmbPelangganKSK.Location = new Point(12, 216);
            cmbPelangganKSK.Size = new Size(200, 30);
            cmbPelangganKSK.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbItemKSK = new Guna2ComboBox();
            cmbItemKSK.Name = "cmbItemKSK";
            cmbItemKSK.Font = Theme.Font(10);
            cmbItemKSK.Location = new Point(220, 216);
            cmbItemKSK.Size = new Size(200, 30);
            cmbItemKSK.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbItemKSK.SelectedIndexChanged += cmbItemKSK_SelectedIndexChanged;
            numQtyKSK = new Guna2NumericUpDown();
            numQtyKSK.Name = "numQtyKSK";
            numQtyKSK.Font = Theme.Font(10);
            numQtyKSK.Location = new Point(428, 216);
            numQtyKSK.Size = new Size(60, 30);
            numQtyKSK.Minimum = 1;
            numQtyKSK.Maximum = 99999;
            numQtyKSK.Value = 1;
            txtHargaJualKSK = BuatTextBox("txtHargaJualKSK", 496, 216, 100, "0");
            btnTambahItemKSK = BuatButton("btnTambahItemKSK", "Tambah Item", 604, 216, btnTambahItemKSK_Click);
            btnTambahItemKSK.Size = new Size(120, 36);

            kskDetailTable = new DataTable();
            kskDetailTable.Columns.Add("ID_KSKDetail", typeof(int));
            kskDetailTable.Columns.Add("ID_Item", typeof(int));
            kskDetailTable.Columns.Add("Nama", typeof(string));
            kskDetailTable.Columns.Add("QtyTitip", typeof(decimal));
            kskDetailTable.Columns.Add("QtyTerjual", typeof(decimal));
            kskDetailTable.Columns.Add("Sisa", typeof(decimal));
            kskDetailTable.Columns.Add("HargaJual", typeof(decimal));
            kskDetailTable.Columns.Add("HPPUnit", typeof(decimal));
            dgvKSKDetail = BuatGrid("dgvKSKDetail", 100);
            dgvKSKDetail.Location = new Point(12, 256);
            dgvKSKDetail.DataSource = kskDetailTable;
            dgvKSKDetail.CellDoubleClick += dgvKSKDetail_CellDoubleClick;
            if (dgvKSKDetail.Columns["ID_KSKDetail"] != null)
                dgvKSKDetail.Columns["ID_KSKDetail"].Visible = false;
            if (dgvKSKDetail.Columns["ID_Item"] != null)
                dgvKSKDetail.Columns["ID_Item"].Visible = false;
            if (dgvKSKDetail.Columns["HPPUnit"] != null)
                dgvKSKDetail.Columns["HPPUnit"].Visible = false;
            if (dgvKSKDetail.Columns["QtyTitip"] != null)
                dgvKSKDetail.Columns["QtyTitip"].DefaultCellStyle.Format = "N0";
            if (dgvKSKDetail.Columns["QtyTerjual"] != null)
                dgvKSKDetail.Columns["QtyTerjual"].DefaultCellStyle.Format = "N0";
            if (dgvKSKDetail.Columns["Sisa"] != null)
                dgvKSKDetail.Columns["Sisa"].DefaultCellStyle.Format = "N0";
            if (dgvKSKDetail.Columns["HargaJual"] != null)
                dgvKSKDetail.Columns["HargaJual"].DefaultCellStyle.Format = "N0";

            txtCatatanKSK = BuatTextBox("txtCatatanKSK", 12, 386, 300, "Catatan (opsional)");

            btnSimpanKSK = BuatButton("btnSimpanKSK", "Simpan KSK", 12, 436, btnSimpanKSK_Click);
            btnClearKSK = BuatButton("btnClearKSK", "Baru", 144, 436, btnClearKSK_Click);
            btnJualKSK = BuatButton("btnJualKSK", "Catat Penjualan Agen", 276, 436, btnJualKSK_Click);
            btnJualKSK.Size = new Size(200, 42);
            btnKembaliKSK = BuatButton("btnKembaliKSK", "Terima Kembali", 488, 436, btnKembaliKSK_Click);
            btnKembaliKSK.Size = new Size(160, 42);

            tabKeluar.Controls.Add(txtCariKSK);
            tabKeluar.Controls.Add(dgvKSK);
            tabKeluar.Controls.Add(BuatLabel("Agen (Pelanggan)", 12, 196));
            tabKeluar.Controls.Add(BuatLabel("Item", 220, 196));
            tabKeluar.Controls.Add(BuatLabel("Qty", 428, 196));
            tabKeluar.Controls.Add(BuatLabel("Harga Jual", 496, 196));
            tabKeluar.Controls.Add(cmbPelangganKSK);
            tabKeluar.Controls.Add(cmbItemKSK);
            tabKeluar.Controls.Add(numQtyKSK);
            tabKeluar.Controls.Add(txtHargaJualKSK);
            tabKeluar.Controls.Add(btnTambahItemKSK);
            tabKeluar.Controls.Add(dgvKSKDetail);
            tabKeluar.Controls.Add(BuatLabel("Catatan", 12, 366));
            tabKeluar.Controls.Add(txtCatatanKSK);
            tabKeluar.Controls.Add(btnSimpanKSK);
            tabKeluar.Controls.Add(btnClearKSK);
            tabKeluar.Controls.Add(btnJualKSK);
            tabKeluar.Controls.Add(btnKembaliKSK);
            PerbaruiTombolKSK();
        }

        private void TerapkanTema()
        {
            this.BackColor = Theme.Background;
            this.Font = Theme.Font(10);
            Theme.StyleTabControl(tabControl);
            Theme.StyleGrid(dgvKSM);
            Theme.StyleGrid(dgvKSMDetail);
            Theme.StyleGrid(dgvKSK);
            Theme.StyleGrid(dgvKSKDetail);
            Theme.StylePrimaryButton(btnSimpanKSM);
            Theme.StyleDangerButton(btnBatalKSM);
            Theme.StyleGhostButton(btnClearKSM);
            Theme.StyleSuccessButton(btnJualKSM);
            Theme.StylePrimaryButton(btnSimpanKSK);
            Theme.StyleGhostButton(btnClearKSK);
            Theme.StyleSuccessButton(btnJualKSK);
            Theme.StylePrimaryButton(btnKembaliKSK);
            Theme.StylePrimaryButton(btnTambahItemKSM);
            Theme.StylePrimaryButton(btnTambahItemKSK);
            Theme.StyleComboBox(cmbSupplierKSM);
            Theme.StyleComboBox(cmbItemKSM);
            Theme.StyleComboBox(cmbPelangganKSK);
            Theme.StyleComboBox(cmbItemKSK);
            Theme.StyleNumericUpDown(numQtyKSM);
            Theme.StyleNumericUpDown(numQtyKSK);
            Theme.StyleTextBox(txtHargaTitipanKSM);
            Theme.StyleTextBox(txtHargaJualKSM);
            Theme.StyleTextBox(txtHargaJualKSK);
            Theme.StyleTextBox(txtCatatanKSM);
            Theme.StyleTextBox(txtCatatanKSK);
            Theme.StyleTextBox(txtCariKSM);
            Theme.StyleTextBox(txtCariKSK);
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

        private static string Sekarang()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        // ==================== MUAT DATA ====================
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
                        cmbSupplierKSM.DisplayMember = "Nama_Supplier";
                        cmbSupplierKSM.ValueMember = "ID_Supplier";
                        cmbSupplierKSM.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat supplier:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

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
                        cmbPelangganKSK.DisplayMember = "Nama";
                        cmbPelangganKSK.ValueMember = "ID_Pelanggan";
                        cmbPelangganKSK.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat pelanggan:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Item titipan hanya Fisik / Rakitan (barang fisik yang punya stok).
        // Dua tabel terpisah supaya pilihan kedua combo tidak saling ikut.
        private void LoadItemComboKSM()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Item, Nama, HargaBeli, HargaJual FROM Item " +
                        "WHERE Tipe IN ('Fisik','Rakitan') ORDER BY Nama", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        itemKSMTable = new DataTable();
                        adapter.Fill(itemKSMTable);
                        cmbItemKSM.DisplayMember = "Nama";
                        cmbItemKSM.ValueMember = "ID_Item";
                        cmbItemKSM.DataSource = itemKSMTable;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat item:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadItemComboKSK()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Item, Nama, HargaBeli, HargaJual FROM Item " +
                        "WHERE Tipe IN ('Fisik','Rakitan') ORDER BY Nama", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        itemKSKTable = new DataTable();
                        adapter.Fill(itemKSKTable);
                        cmbItemKSK.DisplayMember = "Nama";
                        cmbItemKSK.ValueMember = "ID_Item";
                        cmbItemKSK.DataSource = itemKSKTable;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat item:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadKSM()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT k.ID_KSM, k.NoKSM, s.Nama_Supplier AS Supplier, " +
                        "k.Tanggal, " +
                        "COALESCE((SELECT SUM(QtyTitipan) FROM KonsinyasiMasukDetail " +
                        "WHERE ID_KSM = k.ID_KSM), 0) AS QtyTitipan, " +
                        "COALESCE((SELECT SUM(QtyTerjual) FROM KonsinyasiMasukDetail " +
                        "WHERE ID_KSM = k.ID_KSM), 0) AS Terjual, " +
                        "k.Status FROM KonsinyasiMasuk k " +
                        "LEFT JOIN Supplier s ON s.ID_Supplier = k.ID_Supplier " +
                        "ORDER BY k.ID_KSM DESC", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        ksmTable = new DataTable();
                        adapter.Fill(ksmTable);
                        dgvKSM.DataSource = ksmTable;
                        if (dgvKSM.Columns["ID_KSM"] != null)
                            dgvKSM.Columns["ID_KSM"].Visible = false;
                        TerapkanFilterKSM();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat konsinyasi masuk:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TerapkanFilterKSM()
        {
            if (ksmTable == null) return;
            string kata = EscapeFilter(txtCariKSM.Text.Trim());
            ksmTable.DefaultView.RowFilter = kata.Length == 0
                ? ""
                : "NoKSM LIKE '%" + kata + "%' OR Supplier LIKE '%" + kata + "%'";
        }

        private void txtCariKSM_TextChanged(object sender, EventArgs e)
        {
            TerapkanFilterKSM();
        }

        private void LoadKSK()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT k.ID_KSK, k.NoKSK, p.Nama AS Agen, " +
                        "k.Tanggal, " +
                        "COALESCE((SELECT SUM(QtyTitip) FROM KonsinyasiKeluarDetail " +
                        "WHERE ID_KSK = k.ID_KSK), 0) AS QtyTitip, " +
                        "COALESCE((SELECT SUM(QtyTerjual) FROM KonsinyasiKeluarDetail " +
                        "WHERE ID_KSK = k.ID_KSK), 0) AS Terjual, " +
                        "k.Status FROM KonsinyasiKeluar k " +
                        "LEFT JOIN Pelanggan p ON p.ID_Pelanggan = k.ID_Pelanggan " +
                        "ORDER BY k.ID_KSK DESC", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        kskTable = new DataTable();
                        adapter.Fill(kskTable);
                        dgvKSK.DataSource = kskTable;
                        if (dgvKSK.Columns["ID_KSK"] != null)
                            dgvKSK.Columns["ID_KSK"].Visible = false;
                        TerapkanFilterKSK();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat konsinyasi keluar:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TerapkanFilterKSK()
        {
            if (kskTable == null) return;
            string kata = EscapeFilter(txtCariKSK.Text.Trim());
            kskTable.DefaultView.RowFilter = kata.Length == 0
                ? ""
                : "NoKSK LIKE '%" + kata + "%' OR Agen LIKE '%" + kata + "%'";
        }

        private void txtCariKSK_TextChanged(object sender, EventArgs e)
        {
            TerapkanFilterKSK();
        }

        private void cmbItemKSM_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbItemKSM.SelectedValue == null || itemKSMTable == null) return;
            foreach (DataRow baris in itemKSMTable.Rows)
            {
                if (Convert.ToInt32(baris["ID_Item"]) == Convert.ToInt32(cmbItemKSM.SelectedValue))
                {
                    txtHargaTitipanKSM.Text = Convert.ToDecimal(baris["HargaBeli"]).ToString("N0");
                    txtHargaJualKSM.Text = Convert.ToDecimal(baris["HargaJual"]).ToString("N0");
                    return;
                }
            }
        }

        private void cmbItemKSK_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbItemKSK.SelectedValue == null || itemKSKTable == null) return;
            foreach (DataRow baris in itemKSKTable.Rows)
            {
                if (Convert.ToInt32(baris["ID_Item"]) == Convert.ToInt32(cmbItemKSK.SelectedValue))
                {
                    txtHargaJualKSK.Text = Convert.ToDecimal(baris["HargaJual"]).ToString("N0");
                    return;
                }
            }
        }

        // ==================== KSM: FORM ====================
        private void btnTambahItemKSM_Click(object sender, EventArgs e)
        {
            if (!bolehEditKSM)
            {
                MessageBox.Show("KSM ini sudah tersimpan — detailnya tidak bisa diubah.");
                return;
            }
            if (cmbItemKSM.SelectedValue == null)
            {
                MessageBox.Show("Pilih item dulu!");
                return;
            }
            decimal qty = numQtyKSM.Value;
            if (qty <= 0)
            {
                MessageBox.Show("Qty minimal 1!");
                return;
            }
            if (!TryParseDecimal(txtHargaTitipanKSM.Text, out decimal hargaTitipan) || hargaTitipan < 0)
            {
                MessageBox.Show("Harga titipan harus angka 0 atau lebih!");
                return;
            }
            if (!TryParseDecimal(txtHargaJualKSM.Text, out decimal hargaJual) || hargaJual <= 0)
            {
                MessageBox.Show("Harga jual harus angka lebih dari 0!");
                return;
            }
            int idItem = Convert.ToInt32(cmbItemKSM.SelectedValue);
            string nama = Convert.ToString(cmbItemKSM.Text);
            DataRow ada = null;
            foreach (DataRow baris in ksmDetailTable.Rows)
            {
                if (Convert.ToInt32(baris["ID_Item"]) == idItem) { ada = baris; break; }
            }
            if (ada != null)
            {
                ada["QtyTitipan"] = Convert.ToDecimal(ada["QtyTitipan"]) + qty;
                ada["Sisa"] = Convert.ToDecimal(ada["QtyTitipan"]) - Convert.ToDecimal(ada["QtyTerjual"]);
                ada["HargaTitipan"] = hargaTitipan;
                ada["HargaJual"] = hargaJual;
            }
            else
            {
                DataRow baris = ksmDetailTable.NewRow();
                baris["ID_KSMDetail"] = 0;
                baris["ID_Item"] = idItem;
                baris["Nama"] = nama;
                baris["QtyTitipan"] = qty;
                baris["QtyTerjual"] = 0m;
                baris["Sisa"] = qty;
                baris["HargaTitipan"] = hargaTitipan;
                baris["HargaJual"] = hargaJual;
                ksmDetailTable.Rows.Add(baris);
            }
            numQtyKSM.Value = 1;
        }

        private void dgvKSMDetail_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || !bolehEditKSM) return;
            if (MessageBox.Show("Hapus baris ini dari KSM?", "Konfirmasi",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ksmDetailTable.Rows.RemoveAt(e.RowIndex);
            }
        }

        private void dgvKSM_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = dgvKSM.Rows[e.RowIndex];
            int id = Convert.ToInt32(row.Cells["ID_KSM"].Value);
            MuatKSMkeForm(id);
        }

        private void MuatKSMkeForm(int idKSM)
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Supplier, Catatan, Status FROM KonsinyasiMasuk " +
                        "WHERE ID_KSM = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idKSM);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read()) return;
                            idKSMPilihan = idKSM;
                            statusKSMPilihan = Convert.ToString(reader["Status"]);
                            cmbSupplierKSM.SelectedValue = Convert.ToInt32(reader["ID_Supplier"]);
                            txtCatatanKSM.Text = Convert.ToString(reader["Catatan"]);
                        }
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT d.ID_KSMDetail, d.ID_Item, i.Nama, d.QtyTitipan, " +
                        "d.QtyTerjual, d.QtyTitipan - d.QtyTerjual AS Sisa, " +
                        "d.HargaTitipan, d.HargaJual " +
                        "FROM KonsinyasiMasukDetail d INNER JOIN Item i " +
                        "ON i.ID_Item = d.ID_Item WHERE d.ID_KSM = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idKSM);
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        ksmDetailTable.Clear();
                        adapter.Fill(ksmDetailTable);
                    }
                }
                // KSM tersimpan tidak bisa diubah — form dikunci.
                AturEditKSM(false);
                PerbaruiTombolKSM();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat KSM:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AturEditKSM(bool boleh)
        {
            bolehEditKSM = boleh;
            cmbSupplierKSM.Enabled = boleh;
            cmbItemKSM.Enabled = boleh;
            numQtyKSM.Enabled = boleh;
            txtHargaTitipanKSM.Enabled = boleh;
            txtHargaJualKSM.Enabled = boleh;
            btnTambahItemKSM.Enabled = boleh;
            txtCatatanKSM.Enabled = boleh;
        }

        private void PerbaruiTombolKSM()
        {
            if (idKSMPilihan == null)
            {
                btnSimpanKSM.Enabled = true;
                btnBatalKSM.Enabled = false;
                btnJualKSM.Enabled = false;
                return;
            }
            switch (statusKSMPilihan)
            {
                case "Aktif":
                    btnSimpanKSM.Enabled = false;
                    btnBatalKSM.Enabled = true;
                    btnJualKSM.Enabled = true;
                    break;
                default: // Selesai / Dibatalkan
                    btnSimpanKSM.Enabled = false;
                    btnBatalKSM.Enabled = false;
                    btnJualKSM.Enabled = false;
                    break;
            }
        }

        private void ResetFormKSM()
        {
            idKSMPilihan = null;
            statusKSMPilihan = null;
            ksmDetailTable.Clear();
            txtCatatanKSM.Text = "";
            AturEditKSM(true);
            PerbaruiTombolKSM();
        }

        private void btnClearKSM_Click(object sender, EventArgs e)
        {
            ResetFormKSM();
        }

        private string BuatNoKSM()
        {
            string tahun = DateTime.Now.ToString("yyyy");
            long jumlah = 0;
            using (var conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT COUNT(*) FROM KonsinyasiMasuk WHERE NoKSM LIKE @Pola", conn))
                {
                    cmd.Parameters.AddWithValue("@Pola", "KSM-" + tahun + "-%");
                    jumlah = Convert.ToInt64(cmd.ExecuteScalar());
                }
            }
            return "KSM-" + tahun + "-" + (jumlah + 1).ToString("D4");
        }

        private void btnSimpanKSM_Click(object sender, EventArgs e)
        {
            if (!bolehEditKSM || idKSMPilihan != null)
            {
                MessageBox.Show("KSM ini sudah tersimpan — tidak bisa diubah.");
                return;
            }
            if (cmbSupplierKSM.SelectedValue == null)
            {
                MessageBox.Show("Pilih supplier dulu!");
                return;
            }
            if (ksmDetailTable.Rows.Count == 0)
            {
                MessageBox.Show("Tambahkan minimal 1 item dulu!");
                return;
            }
            int idSupplier = Convert.ToInt32(cmbSupplierKSM.SelectedValue);
            string catatan = txtCatatanKSM.Text.Trim();
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO KonsinyasiMasuk (NoKSM, ID_Supplier, Tanggal, " +
                            "Status, Catatan) VALUES (@NoKSM, @ID_Supplier, @Tanggal, " +
                            "'Aktif', @Catatan)", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@NoKSM", BuatNoKSM());
                            cmd.Parameters.AddWithValue("@ID_Supplier", idSupplier);
                            cmd.Parameters.AddWithValue("@Tanggal", Sekarang());
                            cmd.Parameters.AddWithValue("@Catatan", catatan);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand("SELECT last_insert_rowid()", conn, trans))
                        {
                            idKSMPilihan = (int)(long)cmd.ExecuteScalar();
                        }
                        foreach (DataRow baris in ksmDetailTable.Rows)
                        {
                            using (var cmd = new SQLiteCommand(
                                "INSERT INTO KonsinyasiMasukDetail (ID_KSM, ID_Item, " +
                                "QtyTitipan, QtyTerjual, HargaTitipan, HargaJual) " +
                                "VALUES (@ID_KSM, @ID_Item, @QtyTitipan, 0, " +
                                "@HargaTitipan, @HargaJual)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID_KSM", idKSMPilihan.Value);
                                cmd.Parameters.AddWithValue("@ID_Item", baris["ID_Item"]);
                                cmd.Parameters.AddWithValue("@QtyTitipan", baris["QtyTitipan"]);
                                cmd.Parameters.AddWithValue("@HargaTitipan", baris["HargaTitipan"]);
                                cmd.Parameters.AddWithValue("@HargaJual", baris["HargaJual"]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        trans.Commit();
                    }
                }
                int idSimpan = idKSMPilihan.Value;
                LoadKSM();
                MuatKSMkeForm(idSimpan);
                MessageBox.Show("Konsinyasi masuk tersimpan (Aktif). " +
                    "Barang titipan belum masuk stok — hutang baru tercatat " +
                    "saat barang terjual.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menyimpan KSM:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnBatalKSM_Click(object sender, EventArgs e)
        {
            if (idKSMPilihan == null) return;
            if (MessageBox.Show("Batalkan konsinyasi masuk ini?", "Konfirmasi",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    // Hanya Aktif dan belum ada yang terjual yang bisa
                    // dibatalkan — guard di SQL supaya tidak ada race.
                    using (var cmd = new SQLiteCommand(
                        "UPDATE KonsinyasiMasuk SET Status = 'Dibatalkan' " +
                        "WHERE ID_KSM = @ID AND Status = 'Aktif' " +
                        "AND NOT EXISTS (SELECT 1 FROM KonsinyasiMasukDetail " +
                        "WHERE ID_KSM = @ID AND QtyTerjual > 0)", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idKSMPilihan.Value);
                        if (cmd.ExecuteNonQuery() == 0)
                        {
                            MessageBox.Show("KSM tidak bisa dibatalkan — " +
                                "sudah ada barang yang terjual atau status berubah.");
                            return;
                        }
                    }
                }
                LoadKSM();
                ResetFormKSM();
                MessageBox.Show("KSM dibatalkan.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal membatalkan KSM:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Catat Penjualan Titipan: SATU transaksi — QtyTerjual naik,
        // Transaksi tunai + Detail (HPP = qty x HargaTitipan) tercatat,
        // log 'Jual Konsinyasi' ditulis (stok toko TIDAK berubah), Hutang
        // dibuat sebesar qty x HargaTitipan (jatuh tempo +30 hari).
        private void btnJualKSM_Click(object sender, EventArgs e)
        {
            if (idKSMPilihan == null || statusKSMPilihan != "Aktif")
            {
                MessageBox.Show("Pilih KSM yang masih Aktif dulu!");
                return;
            }
            if (dgvKSMDetail.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih baris item di detail dulu yang mau dicatat penjualannya!");
                return;
            }
            DataGridViewRow row = dgvKSMDetail.SelectedRows[0];
            int idDetail = Convert.ToInt32(row.Cells["ID_KSMDetail"].Value);
            if (idDetail <= 0)
            {
                MessageBox.Show("Baris ini belum tersimpan.");
                return;
            }
            int idItem = Convert.ToInt32(row.Cells["ID_Item"].Value);
            string namaItem = Convert.ToString(row.Cells["Nama"].Value);
            decimal sisa = Convert.ToDecimal(row.Cells["Sisa"].Value);
            decimal hargaTitipan = Convert.ToDecimal(row.Cells["HargaTitipan"].Value);
            decimal hargaJual = Convert.ToDecimal(row.Cells["HargaJual"].Value);
            if (sisa <= 0)
            {
                MessageBox.Show("Item ini sudah habis terjual.");
                return;
            }
            FormKonsinyasiJual form = new FormKonsinyasiJual();
            form.Judul = "Catat Penjualan Titipan";
            form.NamaItem = namaItem;
            form.QtyMaks = sisa;
            form.HargaDefault = hargaJual;
            form.TampilkanHarga = true;
            if (form.ShowDialog() != DialogResult.OK) return;
            decimal qty = form.Qty;
            decimal harga = form.HargaJual;
            decimal totalJual = qty * harga;
            decimal totalTitipan = qty * hargaTitipan;
            try
            {
                long idTransaksi = 0;
                long idJual = 0;
                int idSupplier = 0;
                string tanggal = Sekarang();
                string jatuhTempo = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd");
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand(
                            "UPDATE KonsinyasiMasukDetail " +
                            "SET QtyTerjual = QtyTerjual + @Qty " +
                            "WHERE ID_KSMDetail = @ID " +
                            "AND QtyTerjual + @Qty <= QtyTitipan", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Qty", qty);
                            cmd.Parameters.AddWithValue("@ID", idDetail);
                            if (cmd.ExecuteNonQuery() == 0)
                            {
                                trans.Rollback();
                                MessageBox.Show("Qty melebihi sisa titipan. Penjualan dibatalkan.");
                                return;
                            }
                        }
                        using (var cmd = new SQLiteCommand(
                            "SELECT ID_Supplier FROM KonsinyasiMasuk WHERE ID_KSM = @ID",
                            conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", idKSMPilihan.Value);
                            idSupplier = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO Transaksi (Tanggal, Total, Bayar, Kembalian, " +
                            "ID_Pelanggan, Diskon, PoinDipakai, PoinDidapat) " +
                            "VALUES (@Tanggal, @Total, @Bayar, 0, NULL, 0, 0, 0)",
                            conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Tanggal", tanggal);
                            cmd.Parameters.AddWithValue("@Total", totalJual);
                            cmd.Parameters.AddWithValue("@Bayar", totalJual);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand("SELECT last_insert_rowid()", conn, trans))
                        {
                            idTransaksi = (long)cmd.ExecuteScalar();
                        }
                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO Detail_Transaksi (ID_Transaksi, ID_Item, Qty, " +
                            "HargaSatuan, DiskonItem, Subtotal, HPP) " +
                            "VALUES (@ID_Transaksi, @ID_Item, @Qty, @HargaSatuan, 0, " +
                            "@Subtotal, @HPP)", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID_Transaksi", idTransaksi);
                            cmd.Parameters.AddWithValue("@ID_Item", idItem);
                            cmd.Parameters.AddWithValue("@Qty", qty);
                            cmd.Parameters.AddWithValue("@HargaSatuan", harga);
                            cmd.Parameters.AddWithValue("@Subtotal", totalJual);
                            cmd.Parameters.AddWithValue("@HPP", totalTitipan);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO KonsinyasiMasukJual (ID_KSMDetail, Tanggal, " +
                            "Qty, HargaJual, ID_Transaksi) " +
                            "VALUES (@ID_KSMDetail, @Tanggal, @Qty, @HargaJual, @ID_Transaksi)",
                            conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID_KSMDetail", idDetail);
                            cmd.Parameters.AddWithValue("@Tanggal", tanggal);
                            cmd.Parameters.AddWithValue("@Qty", qty);
                            cmd.Parameters.AddWithValue("@HargaJual", harga);
                            cmd.Parameters.AddWithValue("@ID_Transaksi", idTransaksi);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand("SELECT last_insert_rowid()", conn, trans))
                        {
                            idJual = (long)cmd.ExecuteScalar();
                        }
                        // Log penjualan titipan: stok toko TIDAK berubah
                        // (StokSesudah = stok toko saat ini), dan tidak
                        // terikat lokasi mana pun (ID_Lokasi NULL).
                        decimal stokToko = 0;
                        using (var cmd = new SQLiteCommand(
                            "SELECT Stok FROM Item WHERE ID_Item = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", idItem);
                            stokToko = Convert.ToDecimal(cmd.ExecuteScalar());
                        }
                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO StokMovement (ID_Item, Tanggal, Jenis, Qty, " +
                            "StokSesudah, RefID, Keterangan) " +
                            "VALUES (@ID_Item, @Tanggal, 'Jual Konsinyasi', @Qty, " +
                            "@StokSesudah, @RefID, 'Penjualan titipan')", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID_Item", idItem);
                            cmd.Parameters.AddWithValue("@Tanggal", tanggal);
                            cmd.Parameters.AddWithValue("@Qty", -qty);
                            cmd.Parameters.AddWithValue("@StokSesudah", stokToko);
                            cmd.Parameters.AddWithValue("@RefID", idTransaksi.ToString());
                            cmd.ExecuteNonQuery();
                        }
                        // Hutang ke supplier HANYA untuk yang sudah terjual.
                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO Hutang (RefType, RefID, ID_Supplier, Tanggal, " +
                            "JatuhTempo, Total, Sisa, Status) " +
                            "VALUES ('KonsinyasiMasuk', @RefID, @ID_Supplier, @Tanggal, " +
                            "@JatuhTempo, @Total, @Sisa, 'Belum Lunas')", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@RefID", idJual);
                            cmd.Parameters.AddWithValue("@ID_Supplier", idSupplier);
                            cmd.Parameters.AddWithValue("@Tanggal", tanggal);
                            cmd.Parameters.AddWithValue("@JatuhTempo", jatuhTempo);
                            cmd.Parameters.AddWithValue("@Total", totalTitipan);
                            cmd.Parameters.AddWithValue("@Sisa", totalTitipan);
                            cmd.ExecuteNonQuery();
                        }
                        // Kalau semua detail sudah habis terjual, KSM selesai.
                        using (var cmd = new SQLiteCommand(
                            "UPDATE KonsinyasiMasuk SET Status = 'Selesai' " +
                            "WHERE ID_KSM = @ID AND NOT EXISTS (SELECT 1 " +
                            "FROM KonsinyasiMasukDetail WHERE ID_KSM = @ID " +
                            "AND QtyTerjual < QtyTitipan)", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", idKSMPilihan.Value);
                            cmd.ExecuteNonQuery();
                        }
                        // Fase 4: jurnal jual titipan (dalam transaksi yang
                        // sama). Debit Kas [totalJual]; Kredit Pendapatan
                        // [totalJual]; Debit HPP [totalTitipan]; Kredit
                        // Hutang Konsinyasi [totalTitipan] — barang milik
                        // supplier, jadi HPP-nya lawan hutang, bukan
                        // persediaan toko.
                        JurnalHelper.TulisJurnalJualKonsinyasiMasuk(conn, trans,
                            tanggal, idTransaksi, totalJual, totalTitipan);
                        trans.Commit();
                    }
                }
                LoadKSM();
                MuatKSMkeForm(idKSMPilihan.Value);
                MessageBox.Show("Penjualan titipan tercatat.\n" +
                    "Hutang ke supplier: Rp " + totalTitipan.ToString("N0") +
                    " (jatuh tempo +30 hari).");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mencatat penjualan:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==================== KSK: FORM ====================
        private void btnTambahItemKSK_Click(object sender, EventArgs e)
        {
            if (idKSKPilihan != null)
            {
                MessageBox.Show("KSK ini sudah tersimpan — detailnya tidak bisa diubah.");
                return;
            }
            if (cmbItemKSK.SelectedValue == null)
            {
                MessageBox.Show("Pilih item dulu!");
                return;
            }
            decimal qty = numQtyKSK.Value;
            if (qty <= 0)
            {
                MessageBox.Show("Qty minimal 1!");
                return;
            }
            if (!TryParseDecimal(txtHargaJualKSK.Text, out decimal hargaJual) || hargaJual <= 0)
            {
                MessageBox.Show("Harga jual harus angka lebih dari 0!");
                return;
            }
            int idItem = Convert.ToInt32(cmbItemKSK.SelectedValue);
            string nama = Convert.ToString(cmbItemKSK.Text);
            DataRow ada = null;
            foreach (DataRow baris in kskDetailTable.Rows)
            {
                if (Convert.ToInt32(baris["ID_Item"]) == idItem) { ada = baris; break; }
            }
            if (ada != null)
            {
                ada["QtyTitip"] = Convert.ToDecimal(ada["QtyTitip"]) + qty;
                ada["Sisa"] = Convert.ToDecimal(ada["QtyTitip"]) - Convert.ToDecimal(ada["QtyTerjual"]);
                ada["HargaJual"] = hargaJual;
            }
            else
            {
                DataRow baris = kskDetailTable.NewRow();
                baris["ID_KSKDetail"] = 0;
                baris["ID_Item"] = idItem;
                baris["Nama"] = nama;
                baris["QtyTitip"] = qty;
                baris["QtyTerjual"] = 0m;
                baris["Sisa"] = qty;
                baris["HargaJual"] = hargaJual;
                baris["HPPUnit"] = 0m;
                kskDetailTable.Rows.Add(baris);
            }
            numQtyKSK.Value = 1;
        }

        private void dgvKSKDetail_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || idKSKPilihan != null) return;
            if (MessageBox.Show("Hapus baris ini dari KSK?", "Konfirmasi",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                kskDetailTable.Rows.RemoveAt(e.RowIndex);
            }
        }

        private void dgvKSK_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = dgvKSK.Rows[e.RowIndex];
            int id = Convert.ToInt32(row.Cells["ID_KSK"].Value);
            MuatKSKkeForm(id);
        }

        private void MuatKSKkeForm(int idKSK)
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Pelanggan, Catatan, Status FROM KonsinyasiKeluar " +
                        "WHERE ID_KSK = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idKSK);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read()) return;
                            idKSKPilihan = idKSK;
                            statusKSKPilihan = Convert.ToString(reader["Status"]);
                            cmbPelangganKSK.SelectedValue = Convert.ToInt32(reader["ID_Pelanggan"]);
                            txtCatatanKSK.Text = Convert.ToString(reader["Catatan"]);
                        }
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT d.ID_KSKDetail, d.ID_Item, i.Nama, d.QtyTitip, " +
                        "d.QtyTerjual, d.QtyTitip - d.QtyTerjual AS Sisa, " +
                        "d.HargaJual, d.HPPUnit " +
                        "FROM KonsinyasiKeluarDetail d INNER JOIN Item i " +
                        "ON i.ID_Item = d.ID_Item WHERE d.ID_KSK = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idKSK);
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        kskDetailTable.Clear();
                        adapter.Fill(kskDetailTable);
                    }
                }
                AturEditKSK(false);
                PerbaruiTombolKSK();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat KSK:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AturEditKSK(bool boleh)
        {
            cmbPelangganKSK.Enabled = boleh;
            cmbItemKSK.Enabled = boleh;
            numQtyKSK.Enabled = boleh;
            txtHargaJualKSK.Enabled = boleh;
            btnTambahItemKSK.Enabled = boleh;
            txtCatatanKSK.Enabled = boleh;
        }

        private void PerbaruiTombolKSK()
        {
            if (idKSKPilihan == null)
            {
                btnSimpanKSK.Enabled = true;
                btnJualKSK.Enabled = false;
                btnKembaliKSK.Enabled = false;
                return;
            }
            switch (statusKSKPilihan)
            {
                case "Aktif":
                    btnSimpanKSK.Enabled = false;
                    btnJualKSK.Enabled = true;
                    btnKembaliKSK.Enabled = true;
                    break;
                default: // Selesai / Dibatalkan
                    btnSimpanKSK.Enabled = false;
                    btnJualKSK.Enabled = false;
                    btnKembaliKSK.Enabled = false;
                    break;
            }
        }

        private void ResetFormKSK()
        {
            idKSKPilihan = null;
            statusKSKPilihan = null;
            kskDetailTable.Clear();
            txtCatatanKSK.Text = "";
            AturEditKSK(true);
            PerbaruiTombolKSK();
        }

        private void btnClearKSK_Click(object sender, EventArgs e)
        {
            ResetFormKSK();
        }

        private string BuatNoKSK()
        {
            string tahun = DateTime.Now.ToString("yyyy");
            long jumlah = 0;
            using (var conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT COUNT(*) FROM KonsinyasiKeluar WHERE NoKSK LIKE @Pola", conn))
                {
                    cmd.Parameters.AddWithValue("@Pola", "KSK-" + tahun + "-%");
                    jumlah = Convert.ToInt64(cmd.ExecuteScalar());
                }
            }
            return "KSK-" + tahun + "-" + (jumlah + 1).ToString("D4");
        }

        // Simpan KSK: SATU transaksi — per detail: cek stok lokasi 1,
        // konsumsi batch (HPP per unit disimpan di detail), stok total +
        // lokasi berkurang, log 'Konsinyasi Keluar' ditulis. Barang sudah
        // keluar dari stok toko sejak dititipkan (bukan saat laku).
        private void btnSimpanKSK_Click(object sender, EventArgs e)
        {
            if (idKSKPilihan != null)
            {
                MessageBox.Show("KSK ini sudah tersimpan — tidak bisa diubah.");
                return;
            }
            if (cmbPelangganKSK.SelectedValue == null)
            {
                MessageBox.Show("Pilih agen (pelanggan) dulu!");
                return;
            }
            if (kskDetailTable.Rows.Count == 0)
            {
                MessageBox.Show("Tambahkan minimal 1 item dulu!");
                return;
            }
            int idPelanggan = Convert.ToInt32(cmbPelangganKSK.SelectedValue);
            string catatan = txtCatatanKSK.Text.Trim();
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO KonsinyasiKeluar (NoKSK, ID_Pelanggan, Tanggal, " +
                            "Status, Catatan) VALUES (@NoKSK, @ID_Pelanggan, @Tanggal, " +
                            "'Aktif', @Catatan)", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@NoKSK", BuatNoKSK());
                            cmd.Parameters.AddWithValue("@ID_Pelanggan", idPelanggan);
                            cmd.Parameters.AddWithValue("@Tanggal", Sekarang());
                            cmd.Parameters.AddWithValue("@Catatan", catatan);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand("SELECT last_insert_rowid()", conn, trans))
                        {
                            idKSKPilihan = (int)(long)cmd.ExecuteScalar();
                        }
                        string metodeHPP = StokHelper.GetHPPMethod(conn, trans);
                        foreach (DataRow baris in kskDetailTable.Rows)
                        {
                            int idItem = Convert.ToInt32(baris["ID_Item"]);
                            decimal qty = Convert.ToDecimal(baris["QtyTitip"]);
                            decimal hargaJual = Convert.ToDecimal(baris["HargaJual"]);
                            string namaItem = Convert.ToString(baris["Nama"]);
                            decimal stokLokasi = StokHelper.StokLokasiQty(
                                conn, trans, idItem, StokHelper.LokasiUtama);
                            if (qty > stokLokasi)
                            {
                                trans.Rollback();
                                MessageBox.Show("Stok \"" + namaItem + "\" di gudang " +
                                    "utama tidak cukup (tersisa " + stokLokasi +
                                    "). KSK dibatalkan.");
                                return;
                            }
                            StokHelper.HasilKonsumsi konsumsi = StokHelper.KonsumsiBatch(
                                conn, trans, idItem, StokHelper.LokasiUtama,
                                qty, metodeHPP);
                            decimal hppUnit = qty != 0 ? konsumsi.TotalHPP / qty : 0m;
                            using (var cmd = new SQLiteCommand(
                                "INSERT INTO KonsinyasiKeluarDetail (ID_KSK, ID_Item, " +
                                "QtyTitip, QtyTerjual, HargaJual, HPPUnit) " +
                                "VALUES (@ID_KSK, @ID_Item, @QtyTitip, 0, " +
                                "@HargaJual, @HPPUnit)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID_KSK", idKSKPilihan.Value);
                                cmd.Parameters.AddWithValue("@ID_Item", idItem);
                                cmd.Parameters.AddWithValue("@QtyTitip", qty);
                                cmd.Parameters.AddWithValue("@HargaJual", hargaJual);
                                cmd.Parameters.AddWithValue("@HPPUnit", hppUnit);
                                cmd.ExecuteNonQuery();
                            }
                            decimal? stokSesudah = StokHelper.TurunkanStok(
                                conn, trans, idItem, StokHelper.LokasiUtama, qty);
                            if (stokSesudah == null)
                            {
                                trans.Rollback();
                                MessageBox.Show("Stok \"" + namaItem +
                                    "\" tidak cukup (berubah saat menyimpan). KSK dibatalkan.");
                                return;
                            }
                            StokHelper.TulisMovement(conn, trans, idItem,
                                StokHelper.LokasiUtama, Sekarang(),
                                "Konsinyasi Keluar", -qty, stokSesudah.Value,
                                "KSK-" + idKSKPilihan.Value, "Titip ke agen");
                        }
                        trans.Commit();
                    }
                }
                int idSimpan = idKSKPilihan.Value;
                LoadKSK();
                MuatKSKkeForm(idSimpan);
                MessageBox.Show("Konsinyasi keluar tersimpan (Aktif). " +
                    "Stok sudah berkurang — piutang baru tercatat saat agen " +
                    "melaporkan penjualan.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menyimpan KSK:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Catat Penjualan Agen: SATU transaksi — QtyTerjual naik, Transaksi
        // + Detail (HPP dari HPPUnit) tercatat, log 'Jual Konsinyasi
        // Keluar' ditulis (stok tidak berubah), Piutang dibuat ke agen
        // (jatuh tempo +14 hari).
        private void btnJualKSK_Click(object sender, EventArgs e)
        {
            if (idKSKPilihan == null || statusKSKPilihan != "Aktif")
            {
                MessageBox.Show("Pilih KSK yang masih Aktif dulu!");
                return;
            }
            if (dgvKSKDetail.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih baris item di detail dulu yang mau dicatat penjualannya!");
                return;
            }
            DataGridViewRow row = dgvKSKDetail.SelectedRows[0];
            int idDetail = Convert.ToInt32(row.Cells["ID_KSKDetail"].Value);
            if (idDetail <= 0)
            {
                MessageBox.Show("Baris ini belum tersimpan.");
                return;
            }
            int idItem = Convert.ToInt32(row.Cells["ID_Item"].Value);
            string namaItem = Convert.ToString(row.Cells["Nama"].Value);
            decimal sisa = Convert.ToDecimal(row.Cells["Sisa"].Value);
            decimal hargaJual = Convert.ToDecimal(row.Cells["HargaJual"].Value);
            decimal hppUnit = Convert.ToDecimal(row.Cells["HPPUnit"].Value);
            if (sisa <= 0)
            {
                MessageBox.Show("Item ini sudah habis terjual / kembali.");
                return;
            }
            FormKonsinyasiJual form = new FormKonsinyasiJual();
            form.Judul = "Catat Penjualan Agen";
            form.NamaItem = namaItem;
            form.QtyMaks = sisa;
            form.HargaDefault = hargaJual;
            form.TampilkanHarga = true;
            if (form.ShowDialog() != DialogResult.OK) return;
            decimal qty = form.Qty;
            decimal harga = form.HargaJual;
            decimal totalJual = qty * harga;
            decimal totalHPP = qty * hppUnit;
            try
            {
                long idTransaksi = 0;
                int idPelanggan = 0;
                string tanggal = Sekarang();
                string jatuhTempo = DateTime.Now.AddDays(14).ToString("yyyy-MM-dd");
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand(
                            "UPDATE KonsinyasiKeluarDetail " +
                            "SET QtyTerjual = QtyTerjual + @Qty " +
                            "WHERE ID_KSKDetail = @ID " +
                            "AND QtyTerjual + @Qty <= QtyTitip", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Qty", qty);
                            cmd.Parameters.AddWithValue("@ID", idDetail);
                            if (cmd.ExecuteNonQuery() == 0)
                            {
                                trans.Rollback();
                                MessageBox.Show("Qty melebihi sisa titipan. Penjualan dibatalkan.");
                                return;
                            }
                        }
                        using (var cmd = new SQLiteCommand(
                            "SELECT ID_Pelanggan FROM KonsinyasiKeluar WHERE ID_KSK = @ID",
                            conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", idKSKPilihan.Value);
                            idPelanggan = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO Transaksi (Tanggal, Total, Bayar, Kembalian, " +
                            "ID_Pelanggan, Diskon, PoinDipakai, PoinDidapat) " +
                            "VALUES (@Tanggal, @Total, @Bayar, 0, @ID_Pelanggan, 0, 0, 0)",
                            conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Tanggal", tanggal);
                            cmd.Parameters.AddWithValue("@Total", totalJual);
                            cmd.Parameters.AddWithValue("@Bayar", totalJual);
                            cmd.Parameters.AddWithValue("@ID_Pelanggan", idPelanggan);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand("SELECT last_insert_rowid()", conn, trans))
                        {
                            idTransaksi = (long)cmd.ExecuteScalar();
                        }
                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO Detail_Transaksi (ID_Transaksi, ID_Item, Qty, " +
                            "HargaSatuan, DiskonItem, Subtotal, HPP) " +
                            "VALUES (@ID_Transaksi, @ID_Item, @Qty, @HargaSatuan, 0, " +
                            "@Subtotal, @HPP)", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID_Transaksi", idTransaksi);
                            cmd.Parameters.AddWithValue("@ID_Item", idItem);
                            cmd.Parameters.AddWithValue("@Qty", qty);
                            cmd.Parameters.AddWithValue("@HargaSatuan", harga);
                            cmd.Parameters.AddWithValue("@Subtotal", totalJual);
                            cmd.Parameters.AddWithValue("@HPP", totalHPP);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO KonsinyasiKeluarJual (ID_KSKDetail, Tanggal, " +
                            "Qty, HargaJual, ID_Transaksi) " +
                            "VALUES (@ID_KSKDetail, @Tanggal, @Qty, @HargaJual, @ID_Transaksi)",
                            conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID_KSKDetail", idDetail);
                            cmd.Parameters.AddWithValue("@Tanggal", tanggal);
                            cmd.Parameters.AddWithValue("@Qty", qty);
                            cmd.Parameters.AddWithValue("@HargaJual", harga);
                            cmd.Parameters.AddWithValue("@ID_Transaksi", idTransaksi);
                            cmd.ExecuteNonQuery();
                        }
                        decimal stokToko = 0;
                        using (var cmd = new SQLiteCommand(
                            "SELECT Stok FROM Item WHERE ID_Item = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", idItem);
                            stokToko = Convert.ToDecimal(cmd.ExecuteScalar());
                        }
                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO StokMovement (ID_Item, Tanggal, Jenis, Qty, " +
                            "StokSesudah, RefID, Keterangan) " +
                            "VALUES (@ID_Item, @Tanggal, 'Jual Konsinyasi Keluar', @Qty, " +
                            "@StokSesudah, @RefID, 'Penjualan agen')", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID_Item", idItem);
                            cmd.Parameters.AddWithValue("@Tanggal", tanggal);
                            cmd.Parameters.AddWithValue("@Qty", -qty);
                            cmd.Parameters.AddWithValue("@StokSesudah", stokToko);
                            cmd.Parameters.AddWithValue("@RefID", idTransaksi.ToString());
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO Piutang (ID_Transaksi, NoFaktur, ID_Pelanggan, " +
                            "Tanggal, JatuhTempo, Total, Sisa, Status) " +
                            "VALUES (@ID_Transaksi, @NoFaktur, @ID_Pelanggan, " +
                            "@Tanggal, @JatuhTempo, @Total, @Sisa, 'Belum Lunas')",
                            conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID_Transaksi", idTransaksi);
                            cmd.Parameters.AddWithValue("@NoFaktur", "INV-" + idTransaksi);
                            cmd.Parameters.AddWithValue("@ID_Pelanggan", idPelanggan);
                            cmd.Parameters.AddWithValue("@Tanggal", tanggal);
                            cmd.Parameters.AddWithValue("@JatuhTempo", jatuhTempo);
                            cmd.Parameters.AddWithValue("@Total", totalJual);
                            cmd.Parameters.AddWithValue("@Sisa", totalJual);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand(
                            "UPDATE KonsinyasiKeluar SET Status = 'Selesai' " +
                            "WHERE ID_KSK = @ID AND NOT EXISTS (SELECT 1 " +
                            "FROM KonsinyasiKeluarDetail WHERE ID_KSK = @ID " +
                            "AND QtyTerjual < QtyTitip)", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", idKSKPilihan.Value);
                            cmd.ExecuteNonQuery();
                        }
                        // Fase 4: jurnal jual agen (dalam transaksi yang
                        // sama). Debit Piutang Usaha [totalJual]; Kredit
                        // Pendapatan [totalJual]; Debit HPP [totalHPP];
                        // Kredit Persediaan [totalHPP] — stok sudah keluar
                        // saat dititipkan.
                        JurnalHelper.TulisJurnalJualKonsinyasiKeluar(conn, trans,
                            tanggal, idTransaksi, totalJual, totalHPP);
                        trans.Commit();
                    }
                }
                LoadKSK();
                MuatKSKkeForm(idKSKPilihan.Value);
                MessageBox.Show("Penjualan agen tercatat.\n" +
                    "Piutang: Rp " + totalJual.ToString("N0") +
                    " (jatuh tempo +14 hari).");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mencatat penjualan agen:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Terima Kembali: SATU transaksi — QtyTitip berkurang (barang
        // kembali), stok total + lokasi naik, batch baru seharga HPPUnit,
        // log 'Konsinyasi Kembali' ditulis.
        private void btnKembaliKSK_Click(object sender, EventArgs e)
        {
            if (idKSKPilihan == null || statusKSKPilihan != "Aktif")
            {
                MessageBox.Show("Pilih KSK yang masih Aktif dulu!");
                return;
            }
            if (dgvKSKDetail.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih baris item di detail dulu yang mau diterima kembali!");
                return;
            }
            DataGridViewRow row = dgvKSKDetail.SelectedRows[0];
            int idDetail = Convert.ToInt32(row.Cells["ID_KSKDetail"].Value);
            if (idDetail <= 0)
            {
                MessageBox.Show("Baris ini belum tersimpan.");
                return;
            }
            int idItem = Convert.ToInt32(row.Cells["ID_Item"].Value);
            string namaItem = Convert.ToString(row.Cells["Nama"].Value);
            decimal sisa = Convert.ToDecimal(row.Cells["Sisa"].Value);
            decimal hppUnit = Convert.ToDecimal(row.Cells["HPPUnit"].Value);
            if (sisa <= 0)
            {
                MessageBox.Show("Tidak ada sisa titipan untuk item ini.");
                return;
            }
            FormKonsinyasiJual form = new FormKonsinyasiJual();
            form.Judul = "Terima Kembali Barang Titipan";
            form.NamaItem = namaItem;
            form.QtyMaks = sisa;
            form.HargaDefault = 0;
            form.TampilkanHarga = false;
            if (form.ShowDialog() != DialogResult.OK) return;
            decimal qty = form.Qty;
            try
            {
                string tanggal = Sekarang();
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand(
                            "UPDATE KonsinyasiKeluarDetail " +
                            "SET QtyTitip = QtyTitip - @Qty " +
                            "WHERE ID_KSKDetail = @ID " +
                            "AND QtyTitip - QtyTerjual >= @Qty", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Qty", qty);
                            cmd.Parameters.AddWithValue("@ID", idDetail);
                            if (cmd.ExecuteNonQuery() == 0)
                            {
                                trans.Rollback();
                                MessageBox.Show("Qty melebihi sisa titipan yang belum terjual.");
                                return;
                            }
                        }
                        decimal stokSesudah = StokHelper.NaikkanStok(
                            conn, trans, idItem, StokHelper.LokasiUtama, qty);
                        StokHelper.BuatBatch(conn, trans, idItem,
                            StokHelper.LokasiUtama, tanggal, qty, hppUnit,
                            "KonsinyasiKembali", idDetail);
                        StokHelper.TulisMovement(conn, trans, idItem,
                            StokHelper.LokasiUtama, tanggal,
                            "Konsinyasi Kembali", qty, stokSesudah,
                            "KSK-" + idKSKPilihan.Value, "Barang kembali dari agen");
                        using (var cmd = new SQLiteCommand(
                            "UPDATE KonsinyasiKeluar SET Status = 'Selesai' " +
                            "WHERE ID_KSK = @ID AND NOT EXISTS (SELECT 1 " +
                            "FROM KonsinyasiKeluarDetail WHERE ID_KSK = @ID " +
                            "AND QtyTerjual < QtyTitip)", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", idKSKPilihan.Value);
                            cmd.ExecuteNonQuery();
                        }
                        trans.Commit();
                    }
                }
                LoadKSK();
                MuatKSKkeForm(idKSKPilihan.Value);
                MessageBox.Show("Barang kembali diterima — stok bertambah " + qty.ToString("N0") + ".");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menerima kembali barang:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
