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
    /// Fase 2 — single-window shell. FormMain menyimpan sidebar + panel
    /// Dashboard (pnlMain) dan panel konten (pnlContent) yang menampung
    /// satu UserControl aktif: KasirControl, PembelianControl,
    /// PenjualanControl, KonsinyasiControl, PersediaanControl,
    /// MasterDataControl, atau LaporanControl. Kontrol
    /// dibuat sekali (lazy) dan dipakai ulang — keranjang Kasir tetap ada
    /// kalau pengguna pindah tab lalu kembali. Dashboard tetap panel di
    /// FormMain dan di-refresh setiap kali dibuka. FormBayar dan
    /// FormStruk tetap dialog modal.
    ///
    /// Sidebar Fase 3: Dashboard, Kasir, Pembelian, Penjualan,
    /// Konsinyasi, Persediaan, Master Data, Laporan. Tidak ada tombol
    /// untuk modul yang belum ada (tidak ada fake UI).
    /// </summary>
    public partial class FormMain : Form
    {
        // Item dengan stok <= nilai ini dianggap menipis (hanya tipe yang
        // melacak stok: Fisik / Rakitan — Jasa & NonInventori tidak dihitung).
        private const int BatasStokMenipis = 10;

        // True hanya kalau EnsureDatabase + TestConnection berhasil.
        private bool dbOk = false;

        // Disimpan di field supaya Guna2Elipse tidak di-GC.
        private Guna2Elipse formElipse;

        // Kontrol konten — dibuat sekali saat pertama dibuka, dipakai ulang.
        private KasirControl kasirControl;
        private PembelianControl pembelianControl;
        private PenjualanControl penjualanControl;
        private KonsinyasiControl konsinyasiControl;
        private PersediaanControl persediaanControl;
        private KeuanganControl keuanganControl;
        private MasterDataControl masterDataControl;
        private LaporanControl laporanControl;

        public FormMain()
        {
            InitializeComponent();
            TerapkanTema();
            TestDatabase();
            lblTanggal.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy",
                new CultureInfo("id-ID"));
            ShowDashboard();
        }

        private void TerapkanTema()
        {
            Theme.StyleForm(this);
            formElipse = Theme.ApplyRoundedForm(this);
            pnlSidebar.BackColor = Theme.Sidebar;
            pnlMain.BackColor = Theme.Background;
            pnlContent.BackColor = Theme.Background;
            lblBrandTitle.ForeColor = Color.White;
            lblBrandSub.ForeColor = Theme.SidebarMuted;
            lblAdmin.ForeColor = Color.White;
            lblGreeting.ForeColor = Theme.Text;
            lblTanggal.ForeColor = Theme.Muted;
            lblTerlaris.ForeColor = Theme.Text;
            lblStokTitle.ForeColor = Theme.Warning;
            Theme.StyleNavButton(btnNavDashboard, true);
            Theme.StyleNavButton(btnNavKasir, false);
            Theme.StyleNavButton(btnNavPembelian, false);
            Theme.StyleNavButton(btnNavPenjualan, false);
            Theme.StyleNavButton(btnNavKonsinyasi, false);
            Theme.StyleNavButton(btnNavPersediaan, false);
            Theme.StyleNavButton(btnNavKeuangan, false);
            Theme.StyleNavButton(btnNavMasterData, false);
            Theme.StyleNavButton(btnNavLaporan, false);
            Theme.StyleNavButton(btnKeluar, false);
            Theme.StyleCard(pnlKpiOmzet, Theme.Primary);
            Theme.StyleCard(pnlKpiProduk, Theme.Sidebar);
            Theme.StyleCard(pnlKpiTransaksi, Theme.Teal);
            Theme.StyleCard(pnlKpiStok, Theme.Danger);
            lblKpiOmzetTitle.ForeColor = Theme.Muted;
            lblKpiOmzetValue.ForeColor = Theme.Text;
            lblKpiProdukTitle.ForeColor = Theme.Muted;
            lblKpiProdukValue.ForeColor = Theme.Text;
            lblKpiTransaksiTitle.ForeColor = Theme.Muted;
            lblKpiTransaksiValue.ForeColor = Theme.Text;
            lblKpiStokTitle.ForeColor = Theme.Muted;
            lblKpiStokValue.ForeColor = Theme.Text;
            pnlStok.FillColor = Theme.Card;
            lstStokMenipis.BackColor = Theme.Card;
            lstStokMenipis.ForeColor = Theme.Text;
            Theme.StyleGrid(dgvTerlaris);
            Theme.StylePrimaryButton(btnLihatMasterData);
        }

        private void SetActiveNav(Guna2Button aktif)
        {
            Theme.StyleNavButton(btnNavDashboard, aktif == btnNavDashboard);
            Theme.StyleNavButton(btnNavKasir, aktif == btnNavKasir);
            Theme.StyleNavButton(btnNavPembelian, aktif == btnNavPembelian);
            Theme.StyleNavButton(btnNavPenjualan, aktif == btnNavPenjualan);
            Theme.StyleNavButton(btnNavKonsinyasi, aktif == btnNavKonsinyasi);
            Theme.StyleNavButton(btnNavPersediaan, aktif == btnNavPersediaan);
            Theme.StyleNavButton(btnNavKeuangan, aktif == btnNavKeuangan);
            Theme.StyleNavButton(btnNavMasterData, aktif == btnNavMasterData);
            Theme.StyleNavButton(btnNavLaporan, aktif == btnNavLaporan);
            Theme.StyleNavButton(btnKeluar, aktif == btnKeluar);
        }

        /// <summary>
        /// Akses Kasir untuk PenjualanControl ("Muat ke Kasir"): kontrol
        /// dibuat sekali (lazy) sama seperti lewat navigasi, jadi keranjang
        /// yang sudah dimuat tidak hilang kalau pengguna pindah tab.
        /// </summary>
        public KasirControl Kasir
        {
            get
            {
                if (kasirControl == null)
                    kasirControl = new KasirControl();
                return kasirControl;
            }
        }

        /// <summary>
        /// Pindah ke tab Kasir (dipakai PenjualanControl setelah SO dimuat
        /// ke keranjang). RefreshData TIDAK dipanggil di sini — LoadProduk
        /// tidak menyentuh keranjang, tapi panggilannya tidak perlu.
        /// </summary>
        public void TampilkanKasir()
        {
            SetActiveNav(btnNavKasir);
            TampilkanKonten(Kasir);
        }

        private void TestDatabase()
        {
            string error;
            if (!ConnectionManager.EnsureDatabase(out error))
            {
                lblStatus.Text = "Database Error!";
                lblStatus.ForeColor = Theme.Danger;
                MessageBox.Show("Gagal menyiapkan database:\n" + error,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (ConnectionManager.TestConnection(out error))
            {
                lblStatus.Text = "Database OK ✓";
                lblStatus.ForeColor = Theme.Success;
                dbOk = true;
            }
            else
            {
                lblStatus.Text = "Database Error!";
                lblStatus.ForeColor = Theme.Danger;
                MessageBox.Show("Error: " + error,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==================== SHELL: PANEL KONTEN ====================
        private void TampilkanKonten(UserControl kontrol)
        {
            if (!pnlContent.Controls.Contains(kontrol))
            {
                kontrol.Dock = DockStyle.Fill;
                pnlContent.Controls.Add(kontrol);
            }
            foreach (Control c in pnlContent.Controls)
            {
                c.Visible = (c == kontrol);
            }
            pnlContent.Visible = true;
            pnlMain.Visible = false;
        }

        private void ShowDashboard()
        {
            SetActiveNav(btnNavDashboard);
            pnlContent.Visible = false;
            pnlMain.Visible = true;
            LoadDashboard();
        }

        // ==================== DASHBOARD ====================
        private void LoadDashboard()
        {
            if (!dbOk) return;

            try
            {
                // Tanggal disimpan sebagai teks "yyyy-MM-dd HH:mm:ss",
                // jadi filter hari ini memakai LIKE "yyyy-MM-dd%".
                string hariIni = DateTime.Now.ToString("yyyy-MM-dd");

                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();

                    // ----- Omzet + jumlah transaksi hari ini -----
                    using (var cmd = new SQLiteCommand(
                        "SELECT COALESCE(SUM(Total), 0) FROM Transaksi WHERE Tanggal LIKE @Hari", conn))
                    {
                        cmd.Parameters.AddWithValue("@Hari", hariIni + "%");
                        lblKpiOmzetValue.Text = "Rp " +
                            Convert.ToDecimal(cmd.ExecuteScalar()).ToString("N0");
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT COUNT(*) FROM Transaksi WHERE Tanggal LIKE @Hari", conn))
                    {
                        cmd.Parameters.AddWithValue("@Hari", hariIni + "%");
                        lblKpiTransaksiValue.Text = Convert.ToInt64(cmd.ExecuteScalar()).ToString();
                    }

                    // ----- Total item + jumlah stok menipis -----
                    using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Item", conn))
                    {
                        lblKpiProdukValue.Text = Convert.ToInt64(cmd.ExecuteScalar()).ToString();
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT COUNT(*) FROM Item WHERE Stok <= @Batas " +
                        "AND Tipe IN ('Fisik', 'Rakitan')", conn))
                    {
                        cmd.Parameters.AddWithValue("@Batas", BatasStokMenipis);
                        long menipis = Convert.ToInt64(cmd.ExecuteScalar());
                        lblKpiStokValue.Text = menipis.ToString();
                        lblKpiStokValue.ForeColor = menipis > 0 ? Theme.Danger : Theme.Text;
                    }
                }

                LoadTerlaris();
                LoadStokMenipis();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat dashboard:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Item terlaris (5 besar) — satu query JOIN di satu database
        // (dulu JOIN manual karena 3 file .db terpisah).
        private void LoadTerlaris()
        {
            using (var conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT i.Nama AS Nama, SUM(d.Qty) AS Total_Terjual, " +
                    "SUM(d.Subtotal) AS Total_Omzet " +
                    "FROM Detail_Transaksi d " +
                    "INNER JOIN Item i ON i.ID_Item = d.ID_Item " +
                    "GROUP BY i.ID_Item, i.Nama " +
                    "ORDER BY Total_Terjual DESC, Total_Omzet DESC LIMIT 5", conn))
                {
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dgvTerlaris.DataSource = dt;
                }
            }
            if (dgvTerlaris.Columns["Nama"] != null)
                dgvTerlaris.Columns["Nama"].HeaderText = "Produk";
            if (dgvTerlaris.Columns["Total_Terjual"] != null)
                dgvTerlaris.Columns["Total_Terjual"].HeaderText = "Terjual";
            if (dgvTerlaris.Columns["Total_Omzet"] != null)
            {
                dgvTerlaris.Columns["Total_Omzet"].HeaderText = "Omzet";
                dgvTerlaris.Columns["Total_Omzet"].DefaultCellStyle.Format = "N0";
            }
        }

        private void LoadStokMenipis()
        {
            lstStokMenipis.Items.Clear();
            using (var conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT Nama, Stok FROM Item WHERE Stok <= @Batas " +
                    "AND Tipe IN ('Fisik', 'Rakitan') ORDER BY Stok ASC", conn))
                {
                    cmd.Parameters.AddWithValue("@Batas", BatasStokMenipis);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lstStokMenipis.Items.Add(
                                Convert.ToString(reader["Nama"]) +
                                " — tersisa " + Convert.ToDecimal(reader["Stok"]));
                        }
                    }
                }
            }
            if (lstStokMenipis.Items.Count == 0)
                lstStokMenipis.Items.Add("Semua stok aman ✓");
        }

        // ==================== NAVIGASI ====================
        private void btnNavDashboard_Click(object sender, EventArgs e)
        {
            ShowDashboard();
        }

        private void btnNavKasir_Click(object sender, EventArgs e)
        {
            SetActiveNav(btnNavKasir);
            if (kasirControl == null)
                kasirControl = new KasirControl();
            kasirControl.RefreshData();
            TampilkanKonten(kasirControl);
        }

        private void btnNavPembelian_Click(object sender, EventArgs e)
        {
            SetActiveNav(btnNavPembelian);
            if (pembelianControl == null)
                pembelianControl = new PembelianControl();
            pembelianControl.RefreshData();
            TampilkanKonten(pembelianControl);
        }

        private void btnNavPenjualan_Click(object sender, EventArgs e)
        {
            SetActiveNav(btnNavPenjualan);
            if (penjualanControl == null)
            {
                penjualanControl = new PenjualanControl();
                penjualanControl.Shell = this;
            }
            penjualanControl.RefreshData();
            TampilkanKonten(penjualanControl);
        }

        private void btnNavKonsinyasi_Click(object sender, EventArgs e)
        {
            SetActiveNav(btnNavKonsinyasi);
            if (konsinyasiControl == null)
                konsinyasiControl = new KonsinyasiControl();
            konsinyasiControl.RefreshData();
            TampilkanKonten(konsinyasiControl);
        }

        private void btnNavPersediaan_Click(object sender, EventArgs e)
        {
            SetActiveNav(btnNavPersediaan);
            if (persediaanControl == null)
                persediaanControl = new PersediaanControl();
            persediaanControl.RefreshData();
            TampilkanKonten(persediaanControl);
        }

        private void btnNavKeuangan_Click(object sender, EventArgs e)
        {
            SetActiveNav(btnNavKeuangan);
            if (keuanganControl == null)
                keuanganControl = new KeuanganControl();
            keuanganControl.RefreshData();
            TampilkanKonten(keuanganControl);
        }

        private void btnNavMasterData_Click(object sender, EventArgs e)
        {
            SetActiveNav(btnNavMasterData);
            if (masterDataControl == null)
                masterDataControl = new MasterDataControl();
            masterDataControl.RefreshData();
            TampilkanKonten(masterDataControl);
        }

        private void btnNavLaporan_Click(object sender, EventArgs e)
        {
            SetActiveNav(btnNavLaporan);
            if (laporanControl == null)
                laporanControl = new LaporanControl();
            laporanControl.RefreshData();
            TampilkanKonten(laporanControl);
        }

        private void btnLihatMasterData_Click(object sender, EventArgs e)
        {
            btnNavMasterData_Click(sender, e);
        }

        private void btnKeluar_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
