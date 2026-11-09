namespace SistemTokoInventori
{
    partial class FormMain
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.pnlSidebar = new System.Windows.Forms.Panel();
            this.lblBrandTitle = new System.Windows.Forms.Label();
            this.lblBrandSub = new System.Windows.Forms.Label();
            this.btnNavDashboard = new Guna.UI2.WinForms.Guna2Button();
            this.btnNavKasir = new Guna.UI2.WinForms.Guna2Button();
            this.btnNavPembelian = new Guna.UI2.WinForms.Guna2Button();
            this.btnNavPenjualan = new Guna.UI2.WinForms.Guna2Button();
            this.btnNavMasterData = new Guna.UI2.WinForms.Guna2Button();
            this.btnNavLaporan = new Guna.UI2.WinForms.Guna2Button();
            this.btnNavKonsinyasi = new Guna.UI2.WinForms.Guna2Button();
            this.btnNavPersediaan = new Guna.UI2.WinForms.Guna2Button();
            this.btnNavKeuangan = new Guna.UI2.WinForms.Guna2Button();
            this.lblAdmin = new System.Windows.Forms.Label();
            this.btnKeluar = new Guna.UI2.WinForms.Guna2Button();
            this.pnlMain = new System.Windows.Forms.Panel();
            this.pnlContent = new System.Windows.Forms.Panel();
            this.lblGreeting = new System.Windows.Forms.Label();
            this.lblTanggal = new System.Windows.Forms.Label();
            this.pnlKpiOmzet = new Guna.UI2.WinForms.Guna2Panel();
            this.lblKpiOmzetTitle = new System.Windows.Forms.Label();
            this.lblKpiOmzetValue = new System.Windows.Forms.Label();
            this.pnlKpiProduk = new Guna.UI2.WinForms.Guna2Panel();
            this.lblKpiProdukTitle = new System.Windows.Forms.Label();
            this.lblKpiProdukValue = new System.Windows.Forms.Label();
            this.pnlKpiTransaksi = new Guna.UI2.WinForms.Guna2Panel();
            this.lblKpiTransaksiTitle = new System.Windows.Forms.Label();
            this.lblKpiTransaksiValue = new System.Windows.Forms.Label();
            this.pnlKpiStok = new Guna.UI2.WinForms.Guna2Panel();
            this.lblKpiStokTitle = new System.Windows.Forms.Label();
            this.lblKpiStokValue = new System.Windows.Forms.Label();
            this.lblTerlaris = new System.Windows.Forms.Label();
            this.dgvTerlaris = new Guna.UI2.WinForms.Guna2DataGridView();
            this.pnlStok = new Guna.UI2.WinForms.Guna2Panel();
            this.lblStokTitle = new System.Windows.Forms.Label();
            this.lstStokMenipis = new System.Windows.Forms.ListBox();
            this.btnLihatMasterData = new Guna.UI2.WinForms.Guna2Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.pnlSidebar.SuspendLayout();
            this.pnlMain.SuspendLayout();
            this.pnlKpiOmzet.SuspendLayout();
            this.pnlKpiProduk.SuspendLayout();
            this.pnlKpiTransaksi.SuspendLayout();
            this.pnlKpiStok.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTerlaris)).BeginInit();
            this.pnlStok.SuspendLayout();
            this.SuspendLayout();
            //
            // pnlSidebar
            //
            this.pnlSidebar.Controls.Add(this.lblBrandTitle);
            this.pnlSidebar.Controls.Add(this.lblBrandSub);
            this.pnlSidebar.Controls.Add(this.btnNavDashboard);
            this.pnlSidebar.Controls.Add(this.btnNavKasir);
            this.pnlSidebar.Controls.Add(this.btnNavPembelian);
            this.pnlSidebar.Controls.Add(this.btnNavPenjualan);
            this.pnlSidebar.Controls.Add(this.btnNavMasterData);
            this.pnlSidebar.Controls.Add(this.btnNavLaporan);
            this.pnlSidebar.Controls.Add(this.btnNavKonsinyasi);
            this.pnlSidebar.Controls.Add(this.btnNavPersediaan);
            this.pnlSidebar.Controls.Add(this.btnNavKeuangan);
            this.pnlSidebar.Controls.Add(this.lblAdmin);
            this.pnlSidebar.Controls.Add(this.btnKeluar);
            this.pnlSidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlSidebar.Location = new System.Drawing.Point(0, 0);
            this.pnlSidebar.Name = "pnlSidebar";
            this.pnlSidebar.Size = new System.Drawing.Size(210, 650);
            this.pnlSidebar.TabIndex = 0;
            //
            // lblBrandTitle
            //
            this.lblBrandTitle.AutoSize = false;
            this.lblBrandTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblBrandTitle.Location = new System.Drawing.Point(16, 16);
            this.lblBrandTitle.Name = "lblBrandTitle";
            this.lblBrandTitle.Size = new System.Drawing.Size(178, 52);
            this.lblBrandTitle.TabIndex = 0;
            this.lblBrandTitle.Text = "Sistem Toko Inventori";
            //
            // lblBrandSub
            //
            this.lblBrandSub.AutoSize = true;
            this.lblBrandSub.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblBrandSub.Location = new System.Drawing.Point(16, 72);
            this.lblBrandSub.Name = "lblBrandSub";
            this.lblBrandSub.Size = new System.Drawing.Size(120, 15);
            this.lblBrandSub.TabIndex = 1;
            this.lblBrandSub.Text = "Kasir & Inventori Toko";
            //
            // btnNavDashboard
            //
            this.btnNavDashboard.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnNavDashboard.Location = new System.Drawing.Point(16, 116);
            this.btnNavDashboard.Name = "btnNavDashboard";
            this.btnNavDashboard.Size = new System.Drawing.Size(178, 46);
            this.btnNavDashboard.TabIndex = 2;
            this.btnNavDashboard.Text = "  Dashboard";
            this.btnNavDashboard.Click += new System.EventHandler(this.btnNavDashboard_Click);
            //
            // btnNavKasir
            //
            this.btnNavKasir.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnNavKasir.Location = new System.Drawing.Point(16, 164);
            this.btnNavKasir.Name = "btnNavKasir";
            this.btnNavKasir.Size = new System.Drawing.Size(178, 46);
            this.btnNavKasir.TabIndex = 3;
            this.btnNavKasir.Text = "  Kasir";
            this.btnNavKasir.Click += new System.EventHandler(this.btnNavKasir_Click);
            //
            // btnNavPembelian
            //
            this.btnNavPembelian.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnNavPembelian.Location = new System.Drawing.Point(16, 212);
            this.btnNavPembelian.Name = "btnNavPembelian";
            this.btnNavPembelian.Size = new System.Drawing.Size(178, 46);
            this.btnNavPembelian.TabIndex = 4;
            this.btnNavPembelian.Text = "  Pembelian";
            this.btnNavPembelian.Click += new System.EventHandler(this.btnNavPembelian_Click);
            //
            // btnNavPenjualan
            //
            this.btnNavPenjualan.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnNavPenjualan.Location = new System.Drawing.Point(16, 260);
            this.btnNavPenjualan.Name = "btnNavPenjualan";
            this.btnNavPenjualan.Size = new System.Drawing.Size(178, 46);
            this.btnNavPenjualan.TabIndex = 5;
            this.btnNavPenjualan.Text = "  Penjualan";
            this.btnNavPenjualan.Click += new System.EventHandler(this.btnNavPenjualan_Click);
            //
            // btnNavMasterData
            //
            this.btnNavMasterData.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnNavMasterData.Location = new System.Drawing.Point(16, 308);
            this.btnNavMasterData.Name = "btnNavMasterData";
            this.btnNavMasterData.Size = new System.Drawing.Size(178, 46);
            this.btnNavMasterData.TabIndex = 6;
            this.btnNavMasterData.Text = "  Master Data";
            this.btnNavMasterData.Click += new System.EventHandler(this.btnNavMasterData_Click);
            //
            // btnNavLaporan
            //
            this.btnNavLaporan.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnNavLaporan.Location = new System.Drawing.Point(16, 356);
            this.btnNavLaporan.Name = "btnNavLaporan";
            this.btnNavLaporan.Size = new System.Drawing.Size(178, 46);
            this.btnNavLaporan.TabIndex = 7;
            this.btnNavLaporan.Text = "  Laporan";
            this.btnNavLaporan.Click += new System.EventHandler(this.btnNavLaporan_Click);
            //
            // btnNavKonsinyasi
            //
            this.btnNavKonsinyasi.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnNavKonsinyasi.Location = new System.Drawing.Point(16, 404);
            this.btnNavKonsinyasi.Name = "btnNavKonsinyasi";
            this.btnNavKonsinyasi.Size = new System.Drawing.Size(178, 46);
            this.btnNavKonsinyasi.TabIndex = 8;
            this.btnNavKonsinyasi.Text = "  Konsinyasi";
            this.btnNavKonsinyasi.Click += new System.EventHandler(this.btnNavKonsinyasi_Click);
            //
            // btnNavPersediaan
            //
            this.btnNavPersediaan.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnNavPersediaan.Location = new System.Drawing.Point(16, 452);
            this.btnNavPersediaan.Name = "btnNavPersediaan";
            this.btnNavPersediaan.Size = new System.Drawing.Size(178, 46);
            this.btnNavPersediaan.TabIndex = 9;
            this.btnNavPersediaan.Text = "  Persediaan";
            this.btnNavPersediaan.Click += new System.EventHandler(this.btnNavPersediaan_Click);
            //
            // btnNavKeuangan
            //
            this.btnNavKeuangan.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnNavKeuangan.Location = new System.Drawing.Point(16, 500);
            this.btnNavKeuangan.Name = "btnNavKeuangan";
            this.btnNavKeuangan.Size = new System.Drawing.Size(178, 46);
            this.btnNavKeuangan.TabIndex = 10;
            this.btnNavKeuangan.Text = "  Keuangan";
            this.btnNavKeuangan.Click += new System.EventHandler(this.btnNavKeuangan_Click);
            //
            // lblAdmin
            //
            this.lblAdmin.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblAdmin.AutoSize = true;
            this.lblAdmin.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblAdmin.Location = new System.Drawing.Point(16, 556);
            this.lblAdmin.Name = "lblAdmin";
            this.lblAdmin.Size = new System.Drawing.Size(84, 19);
            this.lblAdmin.TabIndex = 11;
            this.lblAdmin.Text = "Admin Toko";
            //
            // btnKeluar
            //
            this.btnKeluar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnKeluar.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnKeluar.Location = new System.Drawing.Point(16, 584);
            this.btnKeluar.Name = "btnKeluar";
            this.btnKeluar.Size = new System.Drawing.Size(178, 46);
            this.btnKeluar.TabIndex = 12;
            this.btnKeluar.Text = "  Keluar";
            this.btnKeluar.Click += new System.EventHandler(this.btnKeluar_Click);
            //
            // pnlMain
            //
            this.pnlMain.Controls.Add(this.lblGreeting);
            this.pnlMain.Controls.Add(this.lblTanggal);
            this.pnlMain.Controls.Add(this.pnlKpiOmzet);
            this.pnlMain.Controls.Add(this.pnlKpiProduk);
            this.pnlMain.Controls.Add(this.pnlKpiTransaksi);
            this.pnlMain.Controls.Add(this.pnlKpiStok);
            this.pnlMain.Controls.Add(this.lblTerlaris);
            this.pnlMain.Controls.Add(this.dgvTerlaris);
            this.pnlMain.Controls.Add(this.pnlStok);
            this.pnlMain.Controls.Add(this.lblStatus);
            this.pnlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMain.Location = new System.Drawing.Point(210, 0);
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.Size = new System.Drawing.Size(890, 650);
            this.pnlMain.TabIndex = 1;
            //
            // pnlContent
            //
            // Panel konten single-window: menampung satu UserControl aktif
            // (Kasir / Master Data / Laporan). Disembunyikan saat Dashboard
            // tampil; Dashboard sendiri tetap pnlMain.
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.Location = new System.Drawing.Point(210, 0);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Size = new System.Drawing.Size(890, 650);
            this.pnlContent.TabIndex = 2;
            this.pnlContent.Visible = false;
            //
            // lblGreeting
            //
            this.lblGreeting.AutoSize = true;
            this.lblGreeting.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            this.lblGreeting.Location = new System.Drawing.Point(24, 18);
            this.lblGreeting.Name = "lblGreeting";
            this.lblGreeting.Size = new System.Drawing.Size(260, 32);
            this.lblGreeting.TabIndex = 0;
            this.lblGreeting.Text = "Selamat datang, Admin";
            //
            // lblTanggal
            //
            this.lblTanggal.AutoSize = true;
            this.lblTanggal.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblTanggal.Location = new System.Drawing.Point(24, 54);
            this.lblTanggal.Name = "lblTanggal";
            this.lblTanggal.Size = new System.Drawing.Size(60, 19);
            this.lblTanggal.TabIndex = 1;
            this.lblTanggal.Text = "Tanggal";
            //
            // pnlKpiOmzet
            //
            this.pnlKpiOmzet.Controls.Add(this.lblKpiOmzetTitle);
            this.pnlKpiOmzet.Controls.Add(this.lblKpiOmzetValue);
            this.pnlKpiOmzet.Location = new System.Drawing.Point(24, 90);
            this.pnlKpiOmzet.Name = "pnlKpiOmzet";
            this.pnlKpiOmzet.Size = new System.Drawing.Size(200, 96);
            this.pnlKpiOmzet.TabIndex = 2;
            //
            // lblKpiOmzetTitle
            //
            this.lblKpiOmzetTitle.AutoSize = true;
            this.lblKpiOmzetTitle.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblKpiOmzetTitle.Location = new System.Drawing.Point(14, 14);
            this.lblKpiOmzetTitle.Name = "lblKpiOmzetTitle";
            this.lblKpiOmzetTitle.Size = new System.Drawing.Size(100, 19);
            this.lblKpiOmzetTitle.TabIndex = 0;
            this.lblKpiOmzetTitle.Text = "Omzet Hari Ini";
            //
            // lblKpiOmzetValue
            //
            this.lblKpiOmzetValue.AutoSize = true;
            this.lblKpiOmzetValue.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblKpiOmzetValue.Location = new System.Drawing.Point(14, 40);
            this.lblKpiOmzetValue.Name = "lblKpiOmzetValue";
            this.lblKpiOmzetValue.Size = new System.Drawing.Size(80, 37);
            this.lblKpiOmzetValue.TabIndex = 1;
            this.lblKpiOmzetValue.Text = "Rp 0";
            //
            // pnlKpiProduk
            //
            this.pnlKpiProduk.Controls.Add(this.lblKpiProdukTitle);
            this.pnlKpiProduk.Controls.Add(this.lblKpiProdukValue);
            this.pnlKpiProduk.Location = new System.Drawing.Point(238, 90);
            this.pnlKpiProduk.Name = "pnlKpiProduk";
            this.pnlKpiProduk.Size = new System.Drawing.Size(200, 96);
            this.pnlKpiProduk.TabIndex = 3;
            //
            // lblKpiProdukTitle
            //
            this.lblKpiProdukTitle.AutoSize = true;
            this.lblKpiProdukTitle.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblKpiProdukTitle.Location = new System.Drawing.Point(14, 14);
            this.lblKpiProdukTitle.Name = "lblKpiProdukTitle";
            this.lblKpiProdukTitle.Size = new System.Drawing.Size(86, 19);
            this.lblKpiProdukTitle.TabIndex = 0;
            this.lblKpiProdukTitle.Text = "Total Produk";
            //
            // lblKpiProdukValue
            //
            this.lblKpiProdukValue.AutoSize = true;
            this.lblKpiProdukValue.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblKpiProdukValue.Location = new System.Drawing.Point(14, 40);
            this.lblKpiProdukValue.Name = "lblKpiProdukValue";
            this.lblKpiProdukValue.Size = new System.Drawing.Size(32, 37);
            this.lblKpiProdukValue.TabIndex = 1;
            this.lblKpiProdukValue.Text = "0";
            //
            // pnlKpiTransaksi
            //
            this.pnlKpiTransaksi.Controls.Add(this.lblKpiTransaksiTitle);
            this.pnlKpiTransaksi.Controls.Add(this.lblKpiTransaksiValue);
            this.pnlKpiTransaksi.Location = new System.Drawing.Point(452, 90);
            this.pnlKpiTransaksi.Name = "pnlKpiTransaksi";
            this.pnlKpiTransaksi.Size = new System.Drawing.Size(200, 96);
            this.pnlKpiTransaksi.TabIndex = 4;
            //
            // lblKpiTransaksiTitle
            //
            this.lblKpiTransaksiTitle.AutoSize = true;
            this.lblKpiTransaksiTitle.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblKpiTransaksiTitle.Location = new System.Drawing.Point(14, 14);
            this.lblKpiTransaksiTitle.Name = "lblKpiTransaksiTitle";
            this.lblKpiTransaksiTitle.Size = new System.Drawing.Size(120, 19);
            this.lblKpiTransaksiTitle.TabIndex = 0;
            this.lblKpiTransaksiTitle.Text = "Transaksi Hari Ini";
            //
            // lblKpiTransaksiValue
            //
            this.lblKpiTransaksiValue.AutoSize = true;
            this.lblKpiTransaksiValue.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblKpiTransaksiValue.Location = new System.Drawing.Point(14, 40);
            this.lblKpiTransaksiValue.Name = "lblKpiTransaksiValue";
            this.lblKpiTransaksiValue.Size = new System.Drawing.Size(32, 37);
            this.lblKpiTransaksiValue.TabIndex = 1;
            this.lblKpiTransaksiValue.Text = "0";
            //
            // pnlKpiStok
            //
            this.pnlKpiStok.Controls.Add(this.lblKpiStokTitle);
            this.pnlKpiStok.Controls.Add(this.lblKpiStokValue);
            this.pnlKpiStok.Location = new System.Drawing.Point(666, 90);
            this.pnlKpiStok.Name = "pnlKpiStok";
            this.pnlKpiStok.Size = new System.Drawing.Size(200, 96);
            this.pnlKpiStok.TabIndex = 5;
            //
            // lblKpiStokTitle
            //
            this.lblKpiStokTitle.AutoSize = true;
            this.lblKpiStokTitle.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblKpiStokTitle.Location = new System.Drawing.Point(14, 14);
            this.lblKpiStokTitle.Name = "lblKpiStokTitle";
            this.lblKpiStokTitle.Size = new System.Drawing.Size(88, 19);
            this.lblKpiStokTitle.TabIndex = 0;
            this.lblKpiStokTitle.Text = "Stok Menipis";
            //
            // lblKpiStokValue
            //
            this.lblKpiStokValue.AutoSize = true;
            this.lblKpiStokValue.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblKpiStokValue.Location = new System.Drawing.Point(14, 40);
            this.lblKpiStokValue.Name = "lblKpiStokValue";
            this.lblKpiStokValue.Size = new System.Drawing.Size(32, 37);
            this.lblKpiStokValue.TabIndex = 1;
            this.lblKpiStokValue.Text = "0";
            //
            // lblTerlaris
            //
            this.lblTerlaris.AutoSize = true;
            this.lblTerlaris.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblTerlaris.Location = new System.Drawing.Point(24, 210);
            this.lblTerlaris.Name = "lblTerlaris";
            this.lblTerlaris.Size = new System.Drawing.Size(120, 21);
            this.lblTerlaris.TabIndex = 6;
            this.lblTerlaris.Text = "Produk Terlaris";
            //
            // dgvTerlaris
            //
            this.dgvTerlaris.AllowUserToAddRows = false;
            this.dgvTerlaris.AllowUserToDeleteRows = false;
            this.dgvTerlaris.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvTerlaris.Location = new System.Drawing.Point(24, 238);
            this.dgvTerlaris.Name = "dgvTerlaris";
            this.dgvTerlaris.ReadOnly = true;
            this.dgvTerlaris.Size = new System.Drawing.Size(560, 340);
            this.dgvTerlaris.TabIndex = 7;
            this.dgvTerlaris.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            //
            // pnlStok
            //
            this.pnlStok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlStok.Controls.Add(this.lblStokTitle);
            this.pnlStok.Controls.Add(this.lstStokMenipis);
            this.pnlStok.Controls.Add(this.btnLihatMasterData);
            this.pnlStok.Location = new System.Drawing.Point(608, 238);
            this.pnlStok.Name = "pnlStok";
            this.pnlStok.Size = new System.Drawing.Size(258, 340);
            this.pnlStok.TabIndex = 8;
            //
            // lblStokTitle
            //
            this.lblStokTitle.AutoSize = true;
            this.lblStokTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblStokTitle.Location = new System.Drawing.Point(14, 12);
            this.lblStokTitle.Name = "lblStokTitle";
            this.lblStokTitle.Size = new System.Drawing.Size(100, 21);
            this.lblStokTitle.TabIndex = 0;
            this.lblStokTitle.Text = "Stok Menipis";
            //
            // lstStokMenipis
            //
            this.lstStokMenipis.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstStokMenipis.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lstStokMenipis.FormattingEnabled = true;
            this.lstStokMenipis.ItemHeight = 17;
            this.lstStokMenipis.Location = new System.Drawing.Point(14, 44);
            this.lstStokMenipis.Name = "lstStokMenipis";
            this.lstStokMenipis.Size = new System.Drawing.Size(230, 238);
            this.lstStokMenipis.TabIndex = 1;
            //
            // btnLihatMasterData
            //
            this.btnLihatMasterData.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnLihatMasterData.Location = new System.Drawing.Point(14, 292);
            this.btnLihatMasterData.Name = "btnLihatMasterData";
            this.btnLihatMasterData.Size = new System.Drawing.Size(230, 36);
            this.btnLihatMasterData.TabIndex = 2;
            this.btnLihatMasterData.Text = "Lihat di Master Data";
            this.btnLihatMasterData.Click += new System.EventHandler(this.btnLihatMasterData_Click);
            //
            // lblStatus
            //
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblStatus.Location = new System.Drawing.Point(24, 612);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(90, 19);
            this.lblStatus.TabIndex = 9;
            this.lblStatus.Text = "Database: ...";
            //
            // FormMain
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1100, 650);
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlMain);
            this.Controls.Add(this.pnlSidebar);
            this.MinimumSize = new System.Drawing.Size(1000, 600);
            this.Name = "FormMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Sistem Toko Inventori";
            this.pnlSidebar.ResumeLayout(false);
            this.pnlSidebar.PerformLayout();
            this.pnlMain.ResumeLayout(false);
            this.pnlMain.PerformLayout();
            this.pnlKpiOmzet.ResumeLayout(false);
            this.pnlKpiOmzet.PerformLayout();
            this.pnlKpiProduk.ResumeLayout(false);
            this.pnlKpiProduk.PerformLayout();
            this.pnlKpiTransaksi.ResumeLayout(false);
            this.pnlKpiTransaksi.PerformLayout();
            this.pnlKpiStok.ResumeLayout(false);
            this.pnlKpiStok.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTerlaris)).EndInit();
            this.pnlStok.ResumeLayout(false);
            this.pnlStok.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlSidebar;
        private System.Windows.Forms.Label lblBrandTitle;
        private System.Windows.Forms.Label lblBrandSub;
        private Guna.UI2.WinForms.Guna2Button btnNavDashboard;
        private Guna.UI2.WinForms.Guna2Button btnNavKasir;
        private Guna.UI2.WinForms.Guna2Button btnNavPembelian;
        private Guna.UI2.WinForms.Guna2Button btnNavPenjualan;
        private Guna.UI2.WinForms.Guna2Button btnNavMasterData;
        private Guna.UI2.WinForms.Guna2Button btnNavLaporan;
        private Guna.UI2.WinForms.Guna2Button btnNavKonsinyasi;
        private Guna.UI2.WinForms.Guna2Button btnNavPersediaan;
        private Guna.UI2.WinForms.Guna2Button btnNavKeuangan;
        private System.Windows.Forms.Label lblAdmin;
        private Guna.UI2.WinForms.Guna2Button btnKeluar;
        private System.Windows.Forms.Panel pnlMain;
        private System.Windows.Forms.Panel pnlContent;
        private System.Windows.Forms.Label lblGreeting;
        private System.Windows.Forms.Label lblTanggal;
        private Guna.UI2.WinForms.Guna2Panel pnlKpiOmzet;
        private System.Windows.Forms.Label lblKpiOmzetTitle;
        private System.Windows.Forms.Label lblKpiOmzetValue;
        private Guna.UI2.WinForms.Guna2Panel pnlKpiProduk;
        private System.Windows.Forms.Label lblKpiProdukTitle;
        private System.Windows.Forms.Label lblKpiProdukValue;
        private Guna.UI2.WinForms.Guna2Panel pnlKpiTransaksi;
        private System.Windows.Forms.Label lblKpiTransaksiTitle;
        private System.Windows.Forms.Label lblKpiTransaksiValue;
        private Guna.UI2.WinForms.Guna2Panel pnlKpiStok;
        private System.Windows.Forms.Label lblKpiStokTitle;
        private System.Windows.Forms.Label lblKpiStokValue;
        private System.Windows.Forms.Label lblTerlaris;
        private Guna.UI2.WinForms.Guna2DataGridView dgvTerlaris;
        private Guna.UI2.WinForms.Guna2Panel pnlStok;
        private System.Windows.Forms.Label lblStokTitle;
        private System.Windows.Forms.ListBox lstStokMenipis;
        private Guna.UI2.WinForms.Guna2Button btnLihatMasterData;
        private System.Windows.Forms.Label lblStatus;
    }
}
