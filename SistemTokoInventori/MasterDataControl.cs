using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace SistemTokoInventori
{
    /// <summary>
    /// Master Data sebagai UserControl (Fase 2, baru): di-host FormMain di
    /// panel konten. Satu Guna2TabControl dengan 4 tab: Item, Supplier,
    /// Pelanggan, Diskon Periode.
    ///
    /// - Item: CRUD + aturan Tipe (Jasa/NonInventori tidak melacak stok;
    ///   Rakitan untuk sekarang seperti Fisik di POS, stoknya dikelola
    ///   fitur Assembly di Fase 3). Perubahan HargaJual dicatat ke
    ///   HargaJualHistory; perubahan stok dicatat ke StokMovement.
    ///   Hapus item yang punya riwayat penjualan diblokir (B4).
    ///   Cetak Barcode: Code 39 digambar sendiri (Barcode39).
    /// - Supplier: CRUD pindahan dari FormInventori (dulu tab Inventori).
    /// - Pelanggan: CRUD + Poin (hanya saldo awal di sini; perolehan /
    ///   pemakaian poin di Kasir adalah Fase 2).
    /// - Diskon Periode: CRUD master saja; penerapan otomatis di Kasir
    ///   adalah Fase 2.
    ///
    /// UI dibangun dari kode (tanpa Designer) supaya semua kontrol dan
    /// wiring-nya bisa dibaca di satu file ini.
    /// </summary>
    public partial class MasterDataControl : UserControl
    {
        private Guna2TabControl tabControl;
        private TabPage tabItem;
        private TabPage tabSupplier;
        private TabPage tabPelanggan;
        private TabPage tabDiskon;

        // ----- Item -----
        private Guna2DataGridView dgvItem;
        private Guna2TextBox txtCariItem;
        private Guna2TextBox txtKode;
        private Guna2TextBox txtNamaItem;
        private Guna2ComboBox cmbTipe;
        private Guna2TextBox txtHargaBeli;
        private Guna2TextBox txtHargaJual;
        private Guna2TextBox txtStokItem;
        private Guna2TextBox txtMinStok;
        private Guna2TextBox txtSatuan;
        private Guna2ComboBox cmbSupplierItem;
        private Guna2TextBox txtBarcode;
        private Guna2Button btnTambahItem;
        private Guna2Button btnUpdateItem;
        private Guna2Button btnHapusItem;
        private Guna2Button btnClearItem;
        private Guna2Button btnCetakBarcode;
        private Guna2Button btnHistoryHarga;
        private Label lblCatatanItem;
        private DataTable itemTable;

        // ----- Supplier -----
        private Guna2DataGridView dgvSupplier;
        private Guna2TextBox txtCariSupplier;
        private Guna2TextBox txtNamaSupplier;
        private Guna2TextBox txtKontak;
        private Guna2TextBox txtAlamat;
        private Guna2Button btnTambahSupplier;
        private Guna2Button btnUpdateSupplier;
        private Guna2Button btnHapusSupplier;
        private DataTable supplierTable;

        // ----- Pelanggan -----
        private Guna2DataGridView dgvPelanggan;
        private Guna2TextBox txtCariPelanggan;
        private Guna2TextBox txtNamaPelanggan;
        private Guna2TextBox txtTelepon;
        private Guna2TextBox txtAlamatPelanggan;
        private Guna2TextBox txtPoin;
        private Guna2Button btnTambahPelanggan;
        private Guna2Button btnUpdatePelanggan;
        private Guna2Button btnHapusPelanggan;
        private Label lblCatatanPoin;
        private DataTable pelangganTable;

        // ----- Diskon Periode -----
        private Guna2DataGridView dgvDiskon;
        private Guna2TextBox txtCariDiskon;
        private Guna2TextBox txtNamaDiskon;
        private Guna2TextBox txtPersen;
        private Guna2DateTimePicker dtpMulai;
        private Guna2DateTimePicker dtpSelesai;
        private CheckBox chkAktif;
        private Guna2Button btnTambahDiskon;
        private Guna2Button btnUpdateDiskon;
        private Guna2Button btnHapusDiskon;
        private Label lblCatatanDiskon;
        private DataTable diskonTable;

        // Data label barcode yang sedang di-preview / dicetak.
        private Bitmap printBitmap;
        private string printNamaItem = "";
        private string printHargaItem = "";
        private string printKodeBarcode = "";

        public MasterDataControl()
        {
            InitializeComponent();
            TerapkanTema();
            LoadSupplierCombo();
            LoadItem();
            LoadSupplier();
            LoadPelanggan();
            LoadDiskon();
        }

        /// <summary>Dipanggil FormMain setiap kali tab Master Data dibuka.</summary>
        public void RefreshData()
        {
            LoadSupplierCombo();
            LoadItem();
            LoadSupplier();
            LoadPelanggan();
            LoadDiskon();
        }

        // ==================== PEMBANGUN UI ====================
        private void InitializeComponent()
        {
            this.tabControl = new Guna.UI2.WinForms.Guna2TabControl();
            this.tabItem = new System.Windows.Forms.TabPage();
            this.tabSupplier = new System.Windows.Forms.TabPage();
            this.tabPelanggan = new System.Windows.Forms.TabPage();
            this.tabDiskon = new System.Windows.Forms.TabPage();
            this.tabControl.Controls.Add(this.tabItem);
            this.tabControl.Controls.Add(this.tabSupplier);
            this.tabControl.Controls.Add(this.tabPelanggan);
            this.tabControl.Controls.Add(this.tabDiskon);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Name = "tabControl";
            this.tabItem.Text = "Item";
            this.tabSupplier.Text = "Supplier";
            this.tabPelanggan.Text = "Pelanggan";
            this.tabDiskon.Text = "Diskon Periode";

            BangunTabItem();
            BangunTabSupplier();
            BangunTabPelanggan();
            BangunTabDiskon();

            this.Controls.Add(this.tabControl);
            this.Name = "MasterDataControl";
            this.Size = new System.Drawing.Size(900, 620);
            var header = Theme.BuildHeader("Master Data", "Kelola item, supplier, pelanggan, dan diskon periode");
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

        private void BangunTabItem()
        {
            txtCariItem = BuatCari("txtCariItem", txtCariItem_TextChanged);
            txtCariItem.PlaceholderText = "Cari kode / nama item...";
            dgvItem = BuatGrid("dgvItem", 220);
            dgvItem.CellClick += dgvItem_CellClick;

            txtKode = BuatTextBox("txtKode", 12, 312, 110, "Otomatis dari nama");
            txtNamaItem = BuatTextBox("txtNamaItem", 140, 312, 220, "Nama item");
            cmbTipe = new Guna2ComboBox();
            cmbTipe.Name = "cmbTipe";
            cmbTipe.Font = Theme.Font(10);
            cmbTipe.Location = new Point(380, 312);
            cmbTipe.Size = new Size(110, 30);
            cmbTipe.Items.AddRange(new object[] { "Fisik", "Rakitan", "NonInventori", "Jasa" });
            cmbTipe.SelectedIndex = 0;
            txtHargaBeli = BuatTextBox("txtHargaBeli", 500, 312, 110, "0");
            txtHargaJual = BuatTextBox("txtHargaJual", 620, 312, 110, "0");
            txtStokItem = BuatTextBox("txtStokItem", 12, 372, 110, "0");
            txtMinStok = BuatTextBox("txtMinStok", 140, 372, 110, "0");
            txtSatuan = BuatTextBox("txtSatuan", 268, 372, 110, "pcs");
            cmbSupplierItem = new Guna2ComboBox();
            cmbSupplierItem.Name = "cmbSupplierItem";
            cmbSupplierItem.Font = Theme.Font(10);
            cmbSupplierItem.Location = new Point(396, 372);
            cmbSupplierItem.Size = new Size(160, 30);
            txtBarcode = BuatTextBox("txtBarcode", 576, 372, 160, "Kosong = pakai Kode");

            btnTambahItem = BuatButton("btnTambahItem", "Tambah", 12, 420, btnTambahItem_Click);
            btnUpdateItem = BuatButton("btnUpdateItem", "Update", 144, 420, btnUpdateItem_Click);
            btnHapusItem = BuatButton("btnHapusItem", "Hapus", 276, 420, btnHapusItem_Click);
            btnClearItem = BuatButton("btnClearItem", "Bersihkan", 408, 420, btnClearItem_Click);
            btnCetakBarcode = BuatButton("btnCetakBarcode", "Cetak Barcode", 700, 12, btnCetakBarcode_Click);
            btnCetakBarcode.Size = new Size(140, 36);
            btnHistoryHarga = BuatButton("btnHistoryHarga", "History Harga", 548, 12, btnHistoryHarga_Click);
            btnHistoryHarga.Size = new Size(140, 36);

            lblCatatanItem = BuatLabel(
                "Tipe Jasa / NonInventori tidak melacak stok (tidak dikurangi di Kasir). " +
                "Tipe Rakitan: stok dikelola fitur Assembly (Fase 3); di Kasir saat ini seperti Fisik.",
                12, 478);
            lblCatatanItem.AutoSize = false;
            lblCatatanItem.Size = new Size(828, 34);

            tabItem.Controls.Add(txtCariItem);
            tabItem.Controls.Add(dgvItem);
            tabItem.Controls.Add(BuatLabel("Kode", 12, 292));
            tabItem.Controls.Add(BuatLabel("Nama", 140, 292));
            tabItem.Controls.Add(BuatLabel("Tipe", 380, 292));
            tabItem.Controls.Add(BuatLabel("Harga Beli", 500, 292));
            tabItem.Controls.Add(BuatLabel("Harga Jual", 620, 292));
            tabItem.Controls.Add(txtKode);
            tabItem.Controls.Add(txtNamaItem);
            tabItem.Controls.Add(cmbTipe);
            tabItem.Controls.Add(txtHargaBeli);
            tabItem.Controls.Add(txtHargaJual);
            tabItem.Controls.Add(BuatLabel("Stok", 12, 352));
            tabItem.Controls.Add(BuatLabel("Min. Stok", 140, 352));
            tabItem.Controls.Add(BuatLabel("Satuan", 268, 352));
            tabItem.Controls.Add(BuatLabel("Supplier", 396, 352));
            tabItem.Controls.Add(BuatLabel("Barcode", 576, 352));
            tabItem.Controls.Add(txtStokItem);
            tabItem.Controls.Add(txtMinStok);
            tabItem.Controls.Add(txtSatuan);
            tabItem.Controls.Add(cmbSupplierItem);
            tabItem.Controls.Add(txtBarcode);
            tabItem.Controls.Add(btnTambahItem);
            tabItem.Controls.Add(btnUpdateItem);
            tabItem.Controls.Add(btnHapusItem);
            tabItem.Controls.Add(btnClearItem);
            tabItem.Controls.Add(btnCetakBarcode);
            tabItem.Controls.Add(btnHistoryHarga);
            tabItem.Controls.Add(lblCatatanItem);
        }

        private void BangunTabSupplier()
        {
            txtCariSupplier = BuatCari("txtCariSupplier", txtCariSupplier_TextChanged);
            txtCariSupplier.PlaceholderText = "Cari supplier...";
            dgvSupplier = BuatGrid("dgvSupplier", 236);
            dgvSupplier.CellClick += dgvSupplier_CellClick;
            txtNamaSupplier = BuatTextBox("txtNamaSupplier", 12, 328, 220, "Nama supplier");
            txtKontak = BuatTextBox("txtKontak", 250, 328, 160, "Kontak");
            txtAlamat = BuatTextBox("txtAlamat", 428, 328, 240, "Alamat");
            btnTambahSupplier = BuatButton("btnTambahSupplier", "Tambah", 12, 372, btnTambahSupplier_Click);
            btnUpdateSupplier = BuatButton("btnUpdateSupplier", "Update", 144, 372, btnUpdateSupplier_Click);
            btnHapusSupplier = BuatButton("btnHapusSupplier", "Hapus", 276, 372, btnHapusSupplier_Click);

            tabSupplier.Controls.Add(txtCariSupplier);
            tabSupplier.Controls.Add(dgvSupplier);
            tabSupplier.Controls.Add(BuatLabel("Nama", 12, 308));
            tabSupplier.Controls.Add(BuatLabel("Kontak", 250, 308));
            tabSupplier.Controls.Add(BuatLabel("Alamat", 428, 308));
            tabSupplier.Controls.Add(txtNamaSupplier);
            tabSupplier.Controls.Add(txtKontak);
            tabSupplier.Controls.Add(txtAlamat);
            tabSupplier.Controls.Add(btnTambahSupplier);
            tabSupplier.Controls.Add(btnUpdateSupplier);
            tabSupplier.Controls.Add(btnHapusSupplier);
        }

        private void BangunTabPelanggan()
        {
            txtCariPelanggan = BuatCari("txtCariPelanggan", txtCariPelanggan_TextChanged);
            txtCariPelanggan.PlaceholderText = "Cari pelanggan...";
            dgvPelanggan = BuatGrid("dgvPelanggan", 220);
            dgvPelanggan.CellClick += dgvPelanggan_CellClick;
            txtNamaPelanggan = BuatTextBox("txtNamaPelanggan", 12, 312, 220, "Nama pelanggan");
            txtTelepon = BuatTextBox("txtTelepon", 250, 312, 160, "Telepon");
            txtAlamatPelanggan = BuatTextBox("txtAlamatPelanggan", 428, 312, 200, "Alamat");
            txtPoin = BuatTextBox("txtPoin", 646, 312, 100, "0");
            btnTambahPelanggan = BuatButton("btnTambahPelanggan", "Tambah", 12, 360, btnTambahPelanggan_Click);
            btnUpdatePelanggan = BuatButton("btnUpdatePelanggan", "Update", 144, 360, btnUpdatePelanggan_Click);
            btnHapusPelanggan = BuatButton("btnHapusPelanggan", "Hapus", 276, 360, btnHapusPelanggan_Click);
            lblCatatanPoin = BuatLabel(
                "Poin didapat = floor(total setelah diskon / Rp 10.000) di Kasir; 1 poin = Rp 100 saat dipakai di Kasir (maks saldo & maks total setelah diskon).",
                12, 418);
            lblCatatanPoin.AutoSize = false;
            lblCatatanPoin.Size = new Size(828, 20);

            tabPelanggan.Controls.Add(txtCariPelanggan);
            tabPelanggan.Controls.Add(dgvPelanggan);
            tabPelanggan.Controls.Add(BuatLabel("Nama", 12, 292));
            tabPelanggan.Controls.Add(BuatLabel("Telepon", 250, 292));
            tabPelanggan.Controls.Add(BuatLabel("Alamat", 428, 292));
            tabPelanggan.Controls.Add(BuatLabel("Poin (saldo awal)", 646, 292));
            tabPelanggan.Controls.Add(txtNamaPelanggan);
            tabPelanggan.Controls.Add(txtTelepon);
            tabPelanggan.Controls.Add(txtAlamatPelanggan);
            tabPelanggan.Controls.Add(txtPoin);
            tabPelanggan.Controls.Add(btnTambahPelanggan);
            tabPelanggan.Controls.Add(btnUpdatePelanggan);
            tabPelanggan.Controls.Add(btnHapusPelanggan);
            tabPelanggan.Controls.Add(lblCatatanPoin);
        }

        private void BangunTabDiskon()
        {
            txtCariDiskon = BuatCari("txtCariDiskon", txtCariDiskon_TextChanged);
            txtCariDiskon.PlaceholderText = "Cari diskon...";
            dgvDiskon = BuatGrid("dgvDiskon", 220);
            dgvDiskon.CellClick += dgvDiskon_CellClick;
            txtNamaDiskon = BuatTextBox("txtNamaDiskon", 12, 312, 220, "Nama diskon");
            txtPersen = BuatTextBox("txtPersen", 250, 312, 110, "0");
            dtpMulai = new Guna2DateTimePicker();
            dtpMulai.Name = "dtpMulai";
            dtpMulai.Font = Theme.Font(10);
            dtpMulai.Location = new Point(380, 312);
            dtpMulai.Size = new Size(140, 30);
            dtpSelesai = new Guna2DateTimePicker();
            dtpSelesai.Name = "dtpSelesai";
            dtpSelesai.Font = Theme.Font(10);
            dtpSelesai.Location = new Point(540, 312);
            dtpSelesai.Size = new Size(140, 30);
            chkAktif = new CheckBox();
            chkAktif.Name = "chkAktif";
            chkAktif.Font = Theme.Font(10, FontStyle.Bold);
            chkAktif.Location = new Point(700, 312);
            chkAktif.Size = new Size(90, 30);
            chkAktif.Text = "Aktif";
            chkAktif.Checked = true;
            btnTambahDiskon = BuatButton("btnTambahDiskon", "Tambah", 12, 360, btnTambahDiskon_Click);
            btnUpdateDiskon = BuatButton("btnUpdateDiskon", "Update", 144, 360, btnUpdateDiskon_Click);
            btnHapusDiskon = BuatButton("btnHapusDiskon", "Hapus", 276, 360, btnHapusDiskon_Click);
            lblCatatanDiskon = BuatLabel(
                "Diskon periode aktif (Aktif=1, hari ini di dalam rentang) otomatis diterapkan di Kasir — persen tertinggi — kalau pelanggan dipilih.",
                12, 418);
            lblCatatanDiskon.AutoSize = false;
            lblCatatanDiskon.Size = new Size(828, 20);

            tabDiskon.Controls.Add(txtCariDiskon);
            tabDiskon.Controls.Add(dgvDiskon);
            tabDiskon.Controls.Add(BuatLabel("Nama", 12, 292));
            tabDiskon.Controls.Add(BuatLabel("Persen (%)", 250, 292));
            tabDiskon.Controls.Add(BuatLabel("Tgl Mulai", 380, 292));
            tabDiskon.Controls.Add(BuatLabel("Tgl Selesai", 540, 292));
            tabDiskon.Controls.Add(txtNamaDiskon);
            tabDiskon.Controls.Add(txtPersen);
            tabDiskon.Controls.Add(dtpMulai);
            tabDiskon.Controls.Add(dtpSelesai);
            tabDiskon.Controls.Add(chkAktif);
            tabDiskon.Controls.Add(btnTambahDiskon);
            tabDiskon.Controls.Add(btnUpdateDiskon);
            tabDiskon.Controls.Add(btnHapusDiskon);
            tabDiskon.Controls.Add(lblCatatanDiskon);
        }

        private void TerapkanTema()
        {
            this.BackColor = Theme.Background;
            this.Font = Theme.Font(10);
            Theme.StyleTabControl(tabControl);
            Theme.StyleGrid(dgvItem);
            Theme.StyleGrid(dgvSupplier);
            Theme.StyleGrid(dgvPelanggan);
            Theme.StyleGrid(dgvDiskon);
            Theme.StylePrimaryButton(btnTambahItem);
            Theme.StyleGhostButton(btnUpdateItem);
            Theme.StyleDangerButton(btnHapusItem);
            Theme.StyleGhostButton(btnClearItem);
            Theme.StylePrimaryButton(btnCetakBarcode);
            Theme.StyleGhostButton(btnHistoryHarga);
            Theme.StylePrimaryButton(btnTambahSupplier);
            Theme.StyleGhostButton(btnUpdateSupplier);
            Theme.StyleDangerButton(btnHapusSupplier);
            Theme.StylePrimaryButton(btnTambahPelanggan);
            Theme.StyleGhostButton(btnUpdatePelanggan);
            Theme.StyleDangerButton(btnHapusPelanggan);
            Theme.StylePrimaryButton(btnTambahDiskon);
            Theme.StyleGhostButton(btnUpdateDiskon);
            Theme.StyleDangerButton(btnHapusDiskon);
            foreach (Guna2TextBox txt in new Guna2TextBox[]
            {
                txtKode, txtNamaItem, txtHargaBeli, txtHargaJual, txtStokItem,
                txtMinStok, txtSatuan, txtBarcode, txtCariItem,
                txtNamaSupplier, txtKontak, txtAlamat, txtCariSupplier,
                txtNamaPelanggan, txtTelepon, txtAlamatPelanggan, txtPoin, txtCariPelanggan,
                txtNamaDiskon, txtPersen, txtCariDiskon
            })
            {
                Theme.StyleTextBox(txt);
            }
            Theme.StyleComboBox(cmbTipe);
            Theme.StyleComboBox(cmbSupplierItem);
            Theme.StyleDatePicker(dtpMulai);
            Theme.StyleDatePicker(dtpSelesai);
            lblCatatanItem.ForeColor = Theme.Muted;
            lblCatatanPoin.ForeColor = Theme.Muted;
            lblCatatanDiskon.ForeColor = Theme.Muted;
            chkAktif.ForeColor = Theme.Text;
        }

        // ==================== HELPER PARSING (B2) ====================
        // B2: CurrentCulture dicoba DULU ("3.500" = Rp 3.500 di id-ID),
        // baru InvariantCulture sebagai fallback ("3500.5").
        private static bool TryParseDecimal(string teks, out decimal nilai)
        {
            teks = (teks ?? "").Trim();
            if (decimal.TryParse(teks, NumberStyles.Number, CultureInfo.CurrentCulture, out nilai))
                return true;
            return decimal.TryParse(teks, NumberStyles.Number, CultureInfo.InvariantCulture, out nilai);
        }

        private static bool TryParseInt(string teks, out int nilai)
        {
            teks = (teks ?? "").Trim();
            if (int.TryParse(teks, NumberStyles.Integer | NumberStyles.AllowThousands,
                CultureInfo.CurrentCulture, out nilai))
                return true;
            return int.TryParse(teks, out nilai);
        }

        private static string EscapeRowFilter(string kata)
        {
            kata = kata.Trim().Replace("'", "''");
            return kata.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
        }

        private static bool MelacakStok(string tipe)
        {
            return tipe != "Jasa" && tipe != "NonInventori";
        }

        private static string Sekarang()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        // ==================== ITEM ====================
        private void LoadSupplierCombo()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter(
                        "SELECT ID_Supplier, Nama_Supplier FROM Supplier ORDER BY Nama_Supplier", conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    cmbSupplierItem.DisplayMember = "Nama_Supplier";
                    cmbSupplierItem.ValueMember = "ID_Supplier";
                    cmbSupplierItem.DataSource = dt;
                    cmbSupplierItem.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat daftar supplier:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadItem()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter(
                        "SELECT i.ID_Item, i.Kode, i.Nama, i.Tipe, i.HargaBeli, i.HargaJual, " +
                        "i.Stok, i.MinStok, i.Satuan, i.ID_Supplier, i.Barcode, " +
                        "COALESCE(s.Nama_Supplier, '-') AS Supplier " +
                        "FROM Item i LEFT JOIN Supplier s ON s.ID_Supplier = i.ID_Supplier " +
                        "ORDER BY i.ID_Item", conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    itemTable = dt;
                    dgvItem.DataSource = itemTable;
                    if (dgvItem.Columns["ID_Item"] != null)
                        dgvItem.Columns["ID_Item"].Visible = false;
                    if (dgvItem.Columns["ID_Supplier"] != null)
                        dgvItem.Columns["ID_Supplier"].Visible = false;
                    if (dgvItem.Columns["HargaBeli"] != null)
                        dgvItem.Columns["HargaBeli"].Visible = false;
                    if (dgvItem.Columns["MinStok"] != null)
                        dgvItem.Columns["MinStok"].Visible = false;
                    if (dgvItem.Columns["Barcode"] != null)
                        dgvItem.Columns["Barcode"].Visible = false;
                    if (dgvItem.Columns["HargaJual"] != null)
                        dgvItem.Columns["HargaJual"].DefaultCellStyle.Format = "N0";
                    TerapkanFilterItem();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat item:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TerapkanFilterItem()
        {
            if (itemTable == null) return;
            string kata = EscapeRowFilter(txtCariItem.Text);
            itemTable.DefaultView.RowFilter = kata.Length == 0
                ? ""
                : "Kode LIKE '%" + kata + "%' OR Nama LIKE '%" + kata + "%'";
        }

        private void txtCariItem_TextChanged(object sender, EventArgs e)
        {
            TerapkanFilterItem();
        }

        // Kode disarankan dari Nama kalau kosong: huruf/angka saja, huruf
        // besar, maks 8 karakter (mis. "Indomie Goreng" -> "INDOMIEG").
        // Kalau sudah dipakai item lain, ditambah "-2", "-3", dst.
        private string SarankanKode(string nama)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in (nama ?? "").ToUpperInvariant())
            {
                if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                    sb.Append(c);
                if (sb.Length >= 8) break;
            }
            string dasar = sb.Length == 0 ? "BRG" : sb.ToString();
            string kode = dasar;
            int i = 2;
            while (KodeDipakai(kode, null))
            {
                kode = dasar + "-" + i;
                i++;
            }
            return kode;
        }

        private bool KodeDipakai(string kode, int? kecualiIdItem)
        {
            using (var conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT COUNT(*) FROM Item WHERE Kode = @Kode " +
                    "AND (@Kecuali IS NULL OR ID_Item <> @Kecuali)", conn))
                {
                    cmd.Parameters.AddWithValue("@Kode", kode);
                    cmd.Parameters.AddWithValue("@Kecuali",
                        kecualiIdItem.HasValue ? (object)kecualiIdItem.Value : DBNull.Value);
                    return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        private bool ValidasiItem(out decimal hargaBeli, out decimal hargaJual,
            out decimal stok, out decimal minStok)
        {
            hargaBeli = 0; hargaJual = 0; stok = 0; minStok = 0;
            if (string.IsNullOrWhiteSpace(txtNamaItem.Text))
            {
                MessageBox.Show("Nama item tidak boleh kosong!");
                txtNamaItem.Focus();
                return false;
            }
            if (cmbTipe.SelectedItem == null)
            {
                MessageBox.Show("Pilih tipe item dulu!");
                return false;
            }
            if (!TryParseDecimal(txtHargaBeli.Text, out hargaBeli) || hargaBeli < 0)
            {
                MessageBox.Show("Harga beli harus angka yang valid (contoh: 3500)!");
                txtHargaBeli.Focus();
                return false;
            }
            if (!TryParseDecimal(txtHargaJual.Text, out hargaJual) || hargaJual < 0)
            {
                MessageBox.Show("Harga jual harus angka yang valid (contoh: 3500)!");
                txtHargaJual.Focus();
                return false;
            }
            if (!TryParseDecimal(txtStokItem.Text, out stok) || stok < 0)
            {
                MessageBox.Show("Stok harus angka 0 atau lebih!");
                txtStokItem.Focus();
                return false;
            }
            if (!TryParseDecimal(txtMinStok.Text, out minStok) || minStok < 0)
            {
                MessageBox.Show("Min. stok harus angka 0 atau lebih!");
                txtMinStok.Focus();
                return false;
            }
            return true;
        }

        private void btnTambahItem_Click(object sender, EventArgs e)
        {
            if (!ValidasiItem(out decimal hargaBeli, out decimal hargaJual,
                out decimal stok, out decimal minStok)) return;

            string kode = txtKode.Text.Trim().ToUpperInvariant();
            if (kode.Length == 0)
                kode = SarankanKode(txtNamaItem.Text);
            if (KodeDipakai(kode, null))
            {
                MessageBox.Show("Kode \"" + kode + "\" sudah dipakai item lain!",
                    "Kode duplikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tipe = Convert.ToString(cmbTipe.SelectedItem);
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        long idBaru;
                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO Item (Kode, Nama, Tipe, HargaBeli, HargaJual, " +
                            "Stok, MinStok, Satuan, ID_Supplier, Barcode) " +
                            "VALUES (@Kode, @Nama, @Tipe, @HargaBeli, @HargaJual, " +
                            "@Stok, @MinStok, @Satuan, @Supplier, @Barcode)", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Kode", kode);
                            cmd.Parameters.AddWithValue("@Nama", txtNamaItem.Text.Trim());
                            cmd.Parameters.AddWithValue("@Tipe", tipe);
                            cmd.Parameters.AddWithValue("@HargaBeli", hargaBeli);
                            cmd.Parameters.AddWithValue("@HargaJual", hargaJual);
                            cmd.Parameters.AddWithValue("@Stok", stok);
                            cmd.Parameters.AddWithValue("@MinStok", minStok);
                            cmd.Parameters.AddWithValue("@Satuan", txtSatuan.Text.Trim());
                            cmd.Parameters.AddWithValue("@Supplier",
                                cmbSupplierItem.SelectedValue ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Barcode",
                                string.IsNullOrWhiteSpace(txtBarcode.Text) ? (object)DBNull.Value : txtBarcode.Text.Trim());
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand("SELECT last_insert_rowid()", conn, trans))
                        {
                            idBaru = (long)cmd.ExecuteScalar();
                        }
                        // Stok awal ikut tercatat di log stok (untuk kartu stok).
                        if (stok != 0 && MelacakStok(tipe))
                        {
                            using (var cmd = new SQLiteCommand(
                                "INSERT INTO StokMovement (ID_Item, Tanggal, Jenis, Qty, " +
                                "StokSesudah, RefID, Keterangan, ID_Lokasi) " +
                                "VALUES (@ID, @Tanggal, 'Masuk', @Qty, @StokSesudah, NULL, 'Stok awal', @Lokasi)",
                                conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID", idBaru);
                                cmd.Parameters.AddWithValue("@Tanggal", Sekarang());
                                cmd.Parameters.AddWithValue("@Qty", stok);
                                cmd.Parameters.AddWithValue("@StokSesudah", stok);
                                cmd.Parameters.AddWithValue("@Lokasi", StokHelper.LokasiUtama);
                                cmd.ExecuteNonQuery();
                            }
                            // Fase 3: stok awal masuk StokLokasi (gudang
                            // utama) + batch awal (HargaBeli item).
                            StokHelper.NaikkanStokLokasi(conn, trans, (int)idBaru,
                                StokHelper.LokasiUtama, stok);
                            StokHelper.BuatBatch(conn, trans, (int)idBaru,
                                StokHelper.LokasiUtama, Sekarang(), stok,
                                hargaBeli, "StokAwal", (int)idBaru);
                        }
                        trans.Commit();
                    }
                    MessageBox.Show("Item ditambahkan! Kode: " + kode);
                    LoadItem();
                    ClearItemForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menambah item:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUpdateItem_Click(object sender, EventArgs e)
        {
            if (dgvItem.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih item di tabel dulu yang mau diupdate!");
                return;
            }
            if (!ValidasiItem(out decimal hargaBeli, out decimal hargaJual,
                out decimal stok, out decimal minStok)) return;

            int id = Convert.ToInt32(dgvItem.SelectedRows[0].Cells["ID_Item"].Value);
            string kode = txtKode.Text.Trim().ToUpperInvariant();
            if (kode.Length == 0)
                kode = SarankanKode(txtNamaItem.Text);
            if (KodeDipakai(kode, id))
            {
                MessageBox.Show("Kode \"" + kode + "\" sudah dipakai item lain!",
                    "Kode duplikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tipe = Convert.ToString(cmbTipe.SelectedItem);
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    // Nilai lama dibaca dulu — untuk riwayat harga & log stok.
                    decimal hargaLama = 0;
                    decimal stokLama = 0;
                    using (var cmd = new SQLiteCommand(
                        "SELECT HargaJual, Stok FROM Item WHERE ID_Item = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", id);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                hargaLama = Convert.ToDecimal(reader["HargaJual"]);
                                stokLama = Convert.ToDecimal(reader["Stok"]);
                            }
                        }
                    }

                    using (var trans = conn.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand(
                            "UPDATE Item SET Kode=@Kode, Nama=@Nama, Tipe=@Tipe, " +
                            "HargaBeli=@HargaBeli, HargaJual=@HargaJual, Stok=@Stok, " +
                            "MinStok=@MinStok, Satuan=@Satuan, ID_Supplier=@Supplier, " +
                            "Barcode=@Barcode WHERE ID_Item=@ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Kode", kode);
                            cmd.Parameters.AddWithValue("@Nama", txtNamaItem.Text.Trim());
                            cmd.Parameters.AddWithValue("@Tipe", tipe);
                            cmd.Parameters.AddWithValue("@HargaBeli", hargaBeli);
                            cmd.Parameters.AddWithValue("@HargaJual", hargaJual);
                            cmd.Parameters.AddWithValue("@Stok", stok);
                            cmd.Parameters.AddWithValue("@MinStok", minStok);
                            cmd.Parameters.AddWithValue("@Satuan", txtSatuan.Text.Trim());
                            cmd.Parameters.AddWithValue("@Supplier",
                                cmbSupplierItem.SelectedValue ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Barcode",
                                string.IsNullOrWhiteSpace(txtBarcode.Text) ? (object)DBNull.Value : txtBarcode.Text.Trim());
                            cmd.Parameters.AddWithValue("@ID", id);
                            cmd.ExecuteNonQuery();
                        }

                        // Harga jual berubah -> catat riwayat harga.
                        if (hargaJual != hargaLama)
                        {
                            using (var cmd = new SQLiteCommand(
                                "INSERT INTO HargaJualHistory (ID_Item, HargaLama, HargaBaru, " +
                                "Tanggal, Keterangan) VALUES (@ID, @Lama, @Baru, @Tanggal, 'Ubah harga')",
                                conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID", id);
                                cmd.Parameters.AddWithValue("@Lama", hargaLama);
                                cmd.Parameters.AddWithValue("@Baru", hargaJual);
                                cmd.Parameters.AddWithValue("@Tanggal", Sekarang());
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Stok berubah -> catat log stok (hanya tipe yang melacak stok).
                        if (stok != stokLama && MelacakStok(tipe))
                        {
                            using (var cmd = new SQLiteCommand(
                                "INSERT INTO StokMovement (ID_Item, Tanggal, Jenis, Qty, " +
                                "StokSesudah, RefID, Keterangan, ID_Lokasi) " +
                                "VALUES (@ID, @Tanggal, 'Penyesuaian', @Qty, @StokSesudah, NULL, " +
                                "'Ubah stok via Master Data', @Lokasi)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID", id);
                                cmd.Parameters.AddWithValue("@Tanggal", Sekarang());
                                cmd.Parameters.AddWithValue("@Qty", stok - stokLama);
                                cmd.Parameters.AddWithValue("@StokSesudah", stok);
                                cmd.Parameters.AddWithValue("@Lokasi", StokHelper.LokasiUtama);
                                cmd.ExecuteNonQuery();
                            }
                            // Fase 3: samakan StokLokasi gudang utama dengan
                            // stok baru + sesuaikan batch (naik = batch
                            // baru, turun = konsumsi batch HPP). Kalau batch
                            // tidak cukup (data lama yang tidak konsisten),
                            // KonsumsiBatch melempar dan transaksi batal.
                            StokHelper.SetStokLokasi(conn, trans, id,
                                StokHelper.LokasiUtama, stok);
                            decimal selisih = stok - stokLama;
                            if (selisih > 0)
                            {
                                StokHelper.BuatBatch(conn, trans, id,
                                    StokHelper.LokasiUtama, Sekarang(), selisih,
                                    hargaBeli, "Penyesuaian", id);
                            }
                            else if (selisih < 0)
                            {
                                StokHelper.KonsumsiBatch(conn, trans, id,
                                    StokHelper.LokasiUtama, -selisih,
                                    StokHelper.GetHPPMethod(conn, trans));
                            }
                        }
                        trans.Commit();
                    }
                    MessageBox.Show("Item diupdate!");
                    LoadItem();
                    ClearItemForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mengupdate item:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnHapusItem_Click(object sender, EventArgs e)
        {
            if (dgvItem.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih item di tabel dulu yang mau dihapus!");
                return;
            }

            int id = Convert.ToInt32(dgvItem.SelectedRows[0].Cells["ID_Item"].Value);
            string nama = dgvItem.SelectedRows[0].Cells["Nama"].Value.ToString();

            // B4: item yang sudah punya riwayat penjualan TIDAK BOLEH dihapus.
            bool adaRiwayat = false;
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT COUNT(*) FROM Detail_Transaksi WHERE ID_Item = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", id);
                        adaRiwayat = Convert.ToInt64(cmd.ExecuteScalar()) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mengecek riwayat transaksi:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (adaRiwayat)
            {
                MessageBox.Show("Item \"" + nama + "\" tidak bisa dihapus karena sudah punya riwayat transaksi.\n" +
                    "Penjualan item ini tercatat di laporan — menghapusnya akan menghilangkan omzet itu.",
                    "Tidak bisa dihapus", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Yakin hapus item \"" + nama + "\"?", "Konfirmasi",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                return;

            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        // Riwayat harga & log stok milik item ini ikut terhapus
                        // (tidak bermakna tanpa itemnya); riwayat penjualan
                        // sudah dicek di atas dan tidak ada.
                        using (var cmd = new SQLiteCommand(
                            "DELETE FROM HargaJualHistory WHERE ID_Item = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", id);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand(
                            "DELETE FROM StokMovement WHERE ID_Item = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", id);
                            cmd.ExecuteNonQuery();
                        }
                        // Fase 3: stok per lokasi + batch + referensi item
                        // di modul gudang ikut terhapus (tidak bermakna
                        // tanpa itemnya; riwayat penjualan sudah dicek
                        // tidak ada, jadi tidak ada transaksi yang hilang).
                        using (var cmd = new SQLiteCommand(
                            "DELETE FROM StokLokasi WHERE ID_Item = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", id);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand(
                            "DELETE FROM StokBatch WHERE ID_Item = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", id);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand(
                            "DELETE FROM RakitanDetail WHERE ID_ItemBahan = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", id);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand(
                            "DELETE FROM RakitanDetail WHERE ID_Rakitan IN " +
                            "(SELECT ID_Rakitan FROM Rakitan WHERE ID_ItemJadi = @ID)", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", id);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand(
                            "DELETE FROM Rakitan WHERE ID_ItemJadi = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", id);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand(
                            "DELETE FROM KonsinyasiMasukDetail WHERE ID_Item = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", id);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand(
                            "DELETE FROM KonsinyasiKeluarDetail WHERE ID_Item = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", id);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand(
                            "DELETE FROM TransferDetail WHERE ID_Item = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", id);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand(
                            "DELETE FROM OpnameDetail WHERE ID_Item = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", id);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand(
                            "DELETE FROM Item WHERE ID_Item = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", id);
                            cmd.ExecuteNonQuery();
                        }
                        trans.Commit();
                    }
                    MessageBox.Show("Item dihapus!");
                    LoadItem();
                    ClearItemForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menghapus item:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClearItem_Click(object sender, EventArgs e)
        {
            ClearItemForm();
        }

        private void dgvItem_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = dgvItem.Rows[e.RowIndex];
            txtKode.Text = Convert.ToString(row.Cells["Kode"].Value);
            txtNamaItem.Text = Convert.ToString(row.Cells["Nama"].Value);
            cmbTipe.SelectedItem = Convert.ToString(row.Cells["Tipe"].Value);
            txtHargaBeli.Text = Convert.ToString(row.Cells["HargaBeli"].Value);
            txtHargaJual.Text = Convert.ToString(row.Cells["HargaJual"].Value);
            txtStokItem.Text = Convert.ToString(row.Cells["Stok"].Value);
            txtMinStok.Text = Convert.ToString(row.Cells["MinStok"].Value);
            txtSatuan.Text = Convert.ToString(row.Cells["Satuan"].Value);
            object supplier = row.Cells["ID_Supplier"].Value;
            cmbSupplierItem.SelectedValue = (supplier == null || supplier == DBNull.Value)
                ? null : supplier;
            txtBarcode.Text = Convert.ToString(row.Cells["Barcode"].Value);
        }

        private void ClearItemForm()
        {
            // Guna2TextBox tidak mewarisi TextBox standar — kosongkan lewat
            // properti Text (bukan Clear()).
            txtKode.Text = "";
            txtNamaItem.Text = "";
            txtHargaBeli.Text = "";
            txtHargaJual.Text = "";
            txtStokItem.Text = "";
            txtMinStok.Text = "";
            txtSatuan.Text = "";
            txtBarcode.Text = "";
            cmbTipe.SelectedIndex = 0;
            cmbSupplierItem.SelectedIndex = -1;
        }

        // ----- Cetak Barcode (Code 39) -----
        // Riwayat perubahan harga jual item yang dipilih (read-only,
        // terbaru dulu). Datanya sudah dicatat otomatis sejak Fase 1
        // setiap kali harga jual diubah (tabel HargaJualHistory).
        private void btnHistoryHarga_Click(object sender, EventArgs e)
        {
            if (dgvItem.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih item di tabel dulu yang mau dilihat riwayat harganya!");
                return;
            }
            DataGridViewRow row = dgvItem.SelectedRows[0];
            int id = Convert.ToInt32(row.Cells["ID_Item"].Value);
            string nama = Convert.ToString(row.Cells["Nama"].Value);
            FormHistoryHarga form = new FormHistoryHarga(id, nama);
            form.ShowDialog();
        }

        private void btnCetakBarcode_Click(object sender, EventArgs e)
        {
            if (dgvItem.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih item di tabel dulu yang mau dicetak barcodenya!");
                return;
            }

            DataGridViewRow row = dgvItem.SelectedRows[0];
            string barcode = Convert.ToString(row.Cells["Barcode"].Value).Trim();
            string kode = Convert.ToString(row.Cells["Kode"].Value).Trim();
            // Barcode diutamakan; kalau kosong, pakai Kode.
            string nilai = barcode.Length > 0 ? barcode : kode;
            string error = Barcode39.Validate(nilai);
            if (error != null)
            {
                MessageBox.Show("Barcode tidak bisa dicetak:\n" + error,
                    "Barcode tidak valid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (printBitmap != null)
                {
                    printBitmap.Dispose();
                    printBitmap = null;
                }
                printBitmap = Barcode39.Render(nilai, 2, 80);
                printNamaItem = Convert.ToString(row.Cells["Nama"].Value);
                printHargaItem = "Rp " + Convert.ToDecimal(row.Cells["HargaJual"].Value).ToString("N0");
                printKodeBarcode = Barcode39.DisplayText(nilai);

                PrintDocument doc = new PrintDocument();
                doc.DocumentName = "Barcode " + printNamaItem;
                doc.PrintPage += PrintLabel_PrintPage;
                PrintPreviewDialog preview = new PrintPreviewDialog();
                preview.Document = doc;
                preview.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mencetak barcode:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintLabel_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            int tengah = e.PageBounds.Width / 2;
            int y = 60;
            using (Font fontNama = Theme.Font(14, FontStyle.Bold))
            using (Font fontBiasa = Theme.Font(12))
            {
                SizeF ukNama = g.MeasureString(printNamaItem, fontNama);
                g.DrawString(printNamaItem, fontNama, Brushes.Black,
                    tengah - ukNama.Width / 2, y);
                y += (int)ukNama.Height + 6;
                SizeF ukHarga = g.MeasureString(printHargaItem, fontBiasa);
                g.DrawString(printHargaItem, fontBiasa, Brushes.Black,
                    tengah - ukHarga.Width / 2, y);
                y += (int)ukHarga.Height + 12;
            }
            if (printBitmap != null)
            {
                g.DrawImage(printBitmap, tengah - printBitmap.Width / 2, y,
                    printBitmap.Width, printBitmap.Height);
                y += printBitmap.Height + 8;
            }
            using (Font fontKode = Theme.Font(12))
            {
                SizeF ukKode = g.MeasureString(printKodeBarcode, fontKode);
                g.DrawString(printKodeBarcode, fontKode, Brushes.Black,
                    tengah - ukKode.Width / 2, y);
            }
            e.HasMorePages = false;
        }

        // ==================== SUPPLIER ====================
        private void LoadSupplier()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter(
                        "SELECT ID_Supplier, Nama_Supplier, Kontak, Alamat FROM Supplier ORDER BY ID_Supplier", conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    supplierTable = dt;
                    dgvSupplier.DataSource = supplierTable;
                    if (dgvSupplier.Columns["ID_Supplier"] != null)
                        dgvSupplier.Columns["ID_Supplier"].Visible = false;
                    TerapkanFilterSupplier();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat supplier:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TerapkanFilterSupplier()
        {
            if (supplierTable == null) return;
            string kata = EscapeRowFilter(txtCariSupplier.Text);
            supplierTable.DefaultView.RowFilter = kata.Length == 0
                ? ""
                : "Nama_Supplier LIKE '%" + kata + "%'";
        }

        private void txtCariSupplier_TextChanged(object sender, EventArgs e)
        {
            TerapkanFilterSupplier();
        }

        private bool ValidasiSupplier()
        {
            if (string.IsNullOrWhiteSpace(txtNamaSupplier.Text))
            {
                MessageBox.Show("Nama supplier tidak boleh kosong!");
                txtNamaSupplier.Focus();
                return false;
            }
            return true;
        }

        private void btnTambahSupplier_Click(object sender, EventArgs e)
        {
            if (!ValidasiSupplier()) return;
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "INSERT INTO Supplier (Nama_Supplier, Kontak, Alamat) VALUES (@Nama, @Kontak, @Alamat)", conn))
                    {
                        cmd.Parameters.AddWithValue("@Nama", txtNamaSupplier.Text.Trim());
                        cmd.Parameters.AddWithValue("@Kontak", txtKontak.Text.Trim());
                        cmd.Parameters.AddWithValue("@Alamat", txtAlamat.Text.Trim());
                        cmd.ExecuteNonQuery();
                    }
                    MessageBox.Show("Supplier ditambahkan!");
                    LoadSupplier();
                    LoadSupplierCombo();
                    ClearSupplierForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menambah supplier:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUpdateSupplier_Click(object sender, EventArgs e)
        {
            if (dgvSupplier.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih supplier di tabel dulu yang mau diupdate!");
                return;
            }
            if (!ValidasiSupplier()) return;

            int id = Convert.ToInt32(dgvSupplier.SelectedRows[0].Cells["ID_Supplier"].Value);
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "UPDATE Supplier SET Nama_Supplier=@Nama, Kontak=@Kontak, Alamat=@Alamat WHERE ID_Supplier=@ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@Nama", txtNamaSupplier.Text.Trim());
                        cmd.Parameters.AddWithValue("@Kontak", txtKontak.Text.Trim());
                        cmd.Parameters.AddWithValue("@Alamat", txtAlamat.Text.Trim());
                        cmd.Parameters.AddWithValue("@ID", id);
                        cmd.ExecuteNonQuery();
                    }
                    MessageBox.Show("Supplier diupdate!");
                    LoadSupplier();
                    LoadSupplierCombo();
                    ClearSupplierForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mengupdate supplier:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnHapusSupplier_Click(object sender, EventArgs e)
        {
            if (dgvSupplier.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih supplier di tabel dulu yang mau dihapus!");
                return;
            }

            int id = Convert.ToInt32(dgvSupplier.SelectedRows[0].Cells["ID_Supplier"].Value);
            string nama = dgvSupplier.SelectedRows[0].Cells["Nama_Supplier"].Value.ToString();

            int jumlahItem = 0;
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT COUNT(*) FROM Item WHERE ID_Supplier = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", id);
                        jumlahItem = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mengecek item:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (jumlahItem > 0)
            {
                MessageBox.Show("Supplier \"" + nama + "\" masih dipakai " + jumlahItem +
                    " item.\nHapus atau pindahkan itemnya dulu sebelum menghapus supplier.",
                    "Tidak bisa dihapus", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Yakin hapus supplier \"" + nama + "\"?", "Konfirmasi",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                return;

            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("DELETE FROM Supplier WHERE ID_Supplier=@ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", id);
                        cmd.ExecuteNonQuery();
                    }
                    MessageBox.Show("Supplier dihapus!");
                    LoadSupplier();
                    LoadSupplierCombo();
                    ClearSupplierForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menghapus supplier:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvSupplier_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = dgvSupplier.Rows[e.RowIndex];
            txtNamaSupplier.Text = Convert.ToString(row.Cells["Nama_Supplier"].Value);
            txtKontak.Text = Convert.ToString(row.Cells["Kontak"].Value);
            txtAlamat.Text = Convert.ToString(row.Cells["Alamat"].Value);
        }

        private void ClearSupplierForm()
        {
            txtNamaSupplier.Text = "";
            txtKontak.Text = "";
            txtAlamat.Text = "";
        }

        // ==================== PELANGGAN ====================
        private void LoadPelanggan()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter(
                        "SELECT ID_Pelanggan, Nama, Telepon, Alamat, Poin FROM Pelanggan ORDER BY ID_Pelanggan", conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    pelangganTable = dt;
                    dgvPelanggan.DataSource = pelangganTable;
                    if (dgvPelanggan.Columns["ID_Pelanggan"] != null)
                        dgvPelanggan.Columns["ID_Pelanggan"].Visible = false;
                    TerapkanFilterPelanggan();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat pelanggan:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TerapkanFilterPelanggan()
        {
            if (pelangganTable == null) return;
            string kata = EscapeRowFilter(txtCariPelanggan.Text);
            pelangganTable.DefaultView.RowFilter = kata.Length == 0
                ? ""
                : "Nama LIKE '%" + kata + "%' OR Telepon LIKE '%" + kata + "%'";
        }

        private void txtCariPelanggan_TextChanged(object sender, EventArgs e)
        {
            TerapkanFilterPelanggan();
        }

        private bool ValidasiPelanggan(out int poin)
        {
            poin = 0;
            if (string.IsNullOrWhiteSpace(txtNamaPelanggan.Text))
            {
                MessageBox.Show("Nama pelanggan tidak boleh kosong!");
                txtNamaPelanggan.Focus();
                return false;
            }
            // Poin kosong = 0 (saldo awal).
            if (string.IsNullOrWhiteSpace(txtPoin.Text))
            {
                poin = 0;
                return true;
            }
            if (!TryParseInt(txtPoin.Text, out poin) || poin < 0)
            {
                MessageBox.Show("Poin harus angka bulat 0 atau lebih!");
                txtPoin.Focus();
                return false;
            }
            return true;
        }

        private void btnTambahPelanggan_Click(object sender, EventArgs e)
        {
            if (!ValidasiPelanggan(out int poin)) return;
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "INSERT INTO Pelanggan (Nama, Telepon, Alamat, Poin) VALUES (@Nama, @Telepon, @Alamat, @Poin)", conn))
                    {
                        cmd.Parameters.AddWithValue("@Nama", txtNamaPelanggan.Text.Trim());
                        cmd.Parameters.AddWithValue("@Telepon", txtTelepon.Text.Trim());
                        cmd.Parameters.AddWithValue("@Alamat", txtAlamatPelanggan.Text.Trim());
                        cmd.Parameters.AddWithValue("@Poin", poin);
                        cmd.ExecuteNonQuery();
                    }
                    MessageBox.Show("Pelanggan ditambahkan!");
                    LoadPelanggan();
                    ClearPelangganForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menambah pelanggan:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUpdatePelanggan_Click(object sender, EventArgs e)
        {
            if (dgvPelanggan.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih pelanggan di tabel dulu yang mau diupdate!");
                return;
            }
            if (!ValidasiPelanggan(out int poin)) return;

            int id = Convert.ToInt32(dgvPelanggan.SelectedRows[0].Cells["ID_Pelanggan"].Value);
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "UPDATE Pelanggan SET Nama=@Nama, Telepon=@Telepon, Alamat=@Alamat, Poin=@Poin WHERE ID_Pelanggan=@ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@Nama", txtNamaPelanggan.Text.Trim());
                        cmd.Parameters.AddWithValue("@Telepon", txtTelepon.Text.Trim());
                        cmd.Parameters.AddWithValue("@Alamat", txtAlamatPelanggan.Text.Trim());
                        cmd.Parameters.AddWithValue("@Poin", poin);
                        cmd.Parameters.AddWithValue("@ID", id);
                        cmd.ExecuteNonQuery();
                    }
                    MessageBox.Show("Pelanggan diupdate!");
                    LoadPelanggan();
                    ClearPelangganForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mengupdate pelanggan:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnHapusPelanggan_Click(object sender, EventArgs e)
        {
            if (dgvPelanggan.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih pelanggan di tabel dulu yang mau dihapus!");
                return;
            }

            int id = Convert.ToInt32(dgvPelanggan.SelectedRows[0].Cells["ID_Pelanggan"].Value);
            string nama = dgvPelanggan.SelectedRows[0].Cells["Nama"].Value.ToString();

            // Pelanggan yang sudah punya transaksi tidak boleh dihapus
            // (riwayat transaksi akan kehilangan pemiliknya).
            bool adaTransaksi = false;
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT COUNT(*) FROM Transaksi WHERE ID_Pelanggan = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", id);
                        adaTransaksi = Convert.ToInt64(cmd.ExecuteScalar()) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mengecek riwayat transaksi:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (adaTransaksi)
            {
                MessageBox.Show("Pelanggan \"" + nama + "\" tidak bisa dihapus karena sudah punya riwayat transaksi.",
                    "Tidak bisa dihapus", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Yakin hapus pelanggan \"" + nama + "\"?", "Konfirmasi",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                return;

            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("DELETE FROM Pelanggan WHERE ID_Pelanggan=@ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", id);
                        cmd.ExecuteNonQuery();
                    }
                    MessageBox.Show("Pelanggan dihapus!");
                    LoadPelanggan();
                    ClearPelangganForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menghapus pelanggan:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvPelanggan_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = dgvPelanggan.Rows[e.RowIndex];
            txtNamaPelanggan.Text = Convert.ToString(row.Cells["Nama"].Value);
            txtTelepon.Text = Convert.ToString(row.Cells["Telepon"].Value);
            txtAlamatPelanggan.Text = Convert.ToString(row.Cells["Alamat"].Value);
            txtPoin.Text = Convert.ToString(row.Cells["Poin"].Value);
        }

        private void ClearPelangganForm()
        {
            txtNamaPelanggan.Text = "";
            txtTelepon.Text = "";
            txtAlamatPelanggan.Text = "";
            txtPoin.Text = "";
        }

        // ==================== DISKON PERIODE ====================
        private void LoadDiskon()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter(
                        "SELECT ID_Diskon, Nama, Persen, TglMulai, TglSelesai, Aktif FROM DiskonPeriode ORDER BY ID_Diskon", conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    diskonTable = dt;
                    dgvDiskon.DataSource = diskonTable;
                    if (dgvDiskon.Columns["ID_Diskon"] != null)
                        dgvDiskon.Columns["ID_Diskon"].Visible = false;
                    TerapkanFilterDiskon();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat diskon periode:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TerapkanFilterDiskon()
        {
            if (diskonTable == null) return;
            string kata = EscapeRowFilter(txtCariDiskon.Text);
            diskonTable.DefaultView.RowFilter = kata.Length == 0
                ? ""
                : "Nama LIKE '%" + kata + "%'";
        }

        private void txtCariDiskon_TextChanged(object sender, EventArgs e)
        {
            TerapkanFilterDiskon();
        }

        private bool ValidasiDiskon(out decimal persen)
        {
            persen = 0;
            if (string.IsNullOrWhiteSpace(txtNamaDiskon.Text))
            {
                MessageBox.Show("Nama diskon tidak boleh kosong!");
                txtNamaDiskon.Focus();
                return false;
            }
            if (!TryParseDecimal(txtPersen.Text, out persen) || persen < 0 || persen > 100)
            {
                MessageBox.Show("Persen harus angka 0 sampai 100!");
                txtPersen.Focus();
                return false;
            }
            if (dtpMulai.Value.Date > dtpSelesai.Value.Date)
            {
                MessageBox.Show("Tanggal mulai tidak boleh lebih besar dari tanggal selesai!");
                return false;
            }
            return true;
        }

        private void btnTambahDiskon_Click(object sender, EventArgs e)
        {
            if (!ValidasiDiskon(out decimal persen)) return;
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "INSERT INTO DiskonPeriode (Nama, Persen, TglMulai, TglSelesai, Aktif) " +
                        "VALUES (@Nama, @Persen, @Mulai, @Selesai, @Aktif)", conn))
                    {
                        cmd.Parameters.AddWithValue("@Nama", txtNamaDiskon.Text.Trim());
                        cmd.Parameters.AddWithValue("@Persen", persen);
                        cmd.Parameters.AddWithValue("@Mulai", dtpMulai.Value.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@Selesai", dtpSelesai.Value.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@Aktif", chkAktif.Checked ? 1 : 0);
                        cmd.ExecuteNonQuery();
                    }
                    MessageBox.Show("Diskon periode ditambahkan!");
                    LoadDiskon();
                    ClearDiskonForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menambah diskon:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUpdateDiskon_Click(object sender, EventArgs e)
        {
            if (dgvDiskon.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih diskon di tabel dulu yang mau diupdate!");
                return;
            }
            if (!ValidasiDiskon(out decimal persen)) return;

            int id = Convert.ToInt32(dgvDiskon.SelectedRows[0].Cells["ID_Diskon"].Value);
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "UPDATE DiskonPeriode SET Nama=@Nama, Persen=@Persen, TglMulai=@Mulai, " +
                        "TglSelesai=@Selesai, Aktif=@Aktif WHERE ID_Diskon=@ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@Nama", txtNamaDiskon.Text.Trim());
                        cmd.Parameters.AddWithValue("@Persen", persen);
                        cmd.Parameters.AddWithValue("@Mulai", dtpMulai.Value.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@Selesai", dtpSelesai.Value.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@Aktif", chkAktif.Checked ? 1 : 0);
                        cmd.Parameters.AddWithValue("@ID", id);
                        cmd.ExecuteNonQuery();
                    }
                    MessageBox.Show("Diskon periode diupdate!");
                    LoadDiskon();
                    ClearDiskonForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mengupdate diskon:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnHapusDiskon_Click(object sender, EventArgs e)
        {
            if (dgvDiskon.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih diskon di tabel dulu yang mau dihapus!");
                return;
            }

            int id = Convert.ToInt32(dgvDiskon.SelectedRows[0].Cells["ID_Diskon"].Value);
            string nama = dgvDiskon.SelectedRows[0].Cells["Nama"].Value.ToString();

            if (MessageBox.Show("Yakin hapus diskon \"" + nama + "\"?", "Konfirmasi",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                return;

            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("DELETE FROM DiskonPeriode WHERE ID_Diskon=@ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", id);
                        cmd.ExecuteNonQuery();
                    }
                    MessageBox.Show("Diskon periode dihapus!");
                    LoadDiskon();
                    ClearDiskonForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menghapus diskon:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvDiskon_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = dgvDiskon.Rows[e.RowIndex];
            txtNamaDiskon.Text = Convert.ToString(row.Cells["Nama"].Value);
            txtPersen.Text = Convert.ToString(row.Cells["Persen"].Value);
            DateTime mulai;
            if (DateTime.TryParseExact(Convert.ToString(row.Cells["TglMulai"].Value),
                "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out mulai))
                dtpMulai.Value = mulai;
            DateTime selesai;
            if (DateTime.TryParseExact(Convert.ToString(row.Cells["TglSelesai"].Value),
                "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out selesai))
                dtpSelesai.Value = selesai;
            chkAktif.Checked = Convert.ToInt32(row.Cells["Aktif"].Value) == 1;
        }

        private void ClearDiskonForm()
        {
            txtNamaDiskon.Text = "";
            txtPersen.Text = "";
            dtpMulai.Value = DateTime.Now.Date;
            dtpSelesai.Value = DateTime.Now.Date;
            chkAktif.Checked = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (printBitmap != null)
                {
                    printBitmap.Dispose();
                    printBitmap = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
