using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace SistemTokoInventori
{
    /// <summary>
    /// Laporan sebagai UserControl (Fase 2): di-host FormMain di panel konten.
    /// Filter tanggal Dari–Sampai, pencarian, dan Total Omzet dipertahankan
    /// dari FormLaporan.
    ///
    /// Perubahan skema (v2): satu database, jadi rekap per item sekarang
    /// satu query JOIN (Item + Detail_Transaksi + Transaksi + Supplier) —
    /// tidak lagi gabung manual tiga database di C#.
    /// </summary>
    public partial class LaporanControl : UserControl
    {
        // Tabel laporan yang tampil di grid — disimpan supaya filter
        // pencarian (RowFilter) bisa diterapkan tanpa query ulang.
        private DataTable laporanTable;

        // Filter tanggal aktif (null = tampilkan semua).
        private DateTime? filterDari;
        private DateTime? filterSampai;

        public LaporanControl()
        {
            InitializeComponent();
            TerapkanTema();
            // Langsung tampilkan laporan saat kontrol dibuka,
            // biar tidak perlu klik dulu saat demo.
            LoadLaporan();
        }

        /// <summary>Dipanggil FormMain setiap kali tab Laporan dibuka.</summary>
        public void RefreshData()
        {
            LoadLaporan();
        }

        private void TerapkanTema()
        {
            this.BackColor = Theme.Background;
            this.Font = Theme.Font(10);
            Theme.StyleGrid(dgvLaporan);
            Theme.StylePrimaryButton(btnLoadLaporan);
            Theme.StylePrimaryButton(btnTerapkan);
            Theme.StyleTextBox(txtCari);
            Theme.StyleDatePicker(dtpDari);
            Theme.StyleDatePicker(dtpSampai);
            // Default filter: awal bulan ini sampai hari ini.
            dtpDari.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dtpSampai.Value = DateTime.Now.Date;
            lblDari.ForeColor = Theme.Text;
            lblSampai.ForeColor = Theme.Text;
            pnlTotalOmzet.FillColor = Theme.SuccessTint;
            lblTotalOmzet.ForeColor = Theme.SuccessDark;
            // Lihat Theme.BuildHeader: label transparan di atas Guna2Panel
            // mewarisi putih — beri BackColor opak = warna panel.
            lblTotalOmzet.BackColor = Theme.SuccessTint;
            var header = Theme.BuildHeader("Laporan", "Produk terlaris dan omzet penjualan");
            this.Controls.Add(header);
        }

        // "Load Laporan" = tampilkan SEMUA (reset filter tanggal + pencarian).
        private void btnLoadLaporan_Click(object sender, EventArgs e)
        {
            filterDari = null;
            filterSampai = null;
            txtCari.Text = "";
            dtpDari.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dtpSampai.Value = DateTime.Now.Date;
            LoadLaporan();
        }

        // Terapkan filter tanggal Dari–Sampai (satu hari penuh: 00:00:00–23:59:59).
        private void btnTerapkan_Click(object sender, EventArgs e)
        {
            if (dtpDari.Value.Date > dtpSampai.Value.Date)
            {
                MessageBox.Show("Tanggal 'Dari' tidak boleh lebih besar dari 'Sampai'!");
                return;
            }
            filterDari = dtpDari.Value.Date;
            filterSampai = dtpSampai.Value.Date;
            LoadLaporan();
        }

        // Pencarian: filter tabel laporan yang sudah dimuat (tanpa query ulang).
        private void TerapkanFilterLaporan()
        {
            if (laporanTable == null) return;
            string kata = txtCari.Text.Trim().Replace("'", "''");
            kata = kata.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
            laporanTable.DefaultView.RowFilter = kata.Length == 0
                ? ""
                : "Nama LIKE '%" + kata + "%'";
        }

        private void txtCari_TextChanged(object sender, EventArgs e)
        {
            TerapkanFilterLaporan();
        }

        private void LoadLaporan()
        {
            try
            {
                // Satu query JOIN di satu database. Tanggal disimpan sebagai
                // teks "yyyy-MM-dd HH:mm:ss" (format lebar-tetap, jadi
                // perbandingan string = perbandingan waktu). LEFT JOIN supaya
                // SEMUA item tampil — yang belum terjual tetap muncul (0).
                // Filter tanggal ada di ON (via EXISTS), BUKAN di WHERE:
                // kalau di WHERE, item yang penjualannya hanya di luar
                // rentang akan hilang dari laporan; di ON, detail di luar
                // rentang tidak ikut JOIN dan itemnya tetap tampil (0).
                // Catatan: SUM() diambil dengan Convert.ToInt32 / ToDecimal
                // (bukan GetInt32/GetDecimal) karena SUM bisa dikembalikan
                // sebagai Int64 / Double oleh System.Data.SQLite.
                string query =
                    "SELECT i.ID_Item, i.Nama, " +
                    "COALESCE(SUM(d.Qty), 0) AS Total_Terjual, " +
                    "COALESCE(SUM(d.Subtotal), 0) AS Total_Omzet, " +
                    "COALESCE(s.Nama_Supplier, '-') AS Nama_Supplier, " +
                    "COALESCE(s.Kontak, '-') AS Kontak_Supplier " +
                    "FROM Item i " +
                    "LEFT JOIN Detail_Transaksi d ON d.ID_Item = i.ID_Item " +
                    "AND (@PakaiFilter = 0 OR EXISTS (SELECT 1 FROM Transaksi t " +
                    "WHERE t.ID_Transaksi = d.ID_Transaksi " +
                    "AND t.Tanggal BETWEEN @Dari AND @Sampai)) " +
                    "LEFT JOIN Supplier s ON s.ID_Supplier = i.ID_Supplier " +
                    "GROUP BY i.ID_Item, i.Nama, s.Nama_Supplier, s.Kontak " +
                    "ORDER BY Total_Terjual DESC, Total_Omzet DESC";

                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        // Kalau filter tanggal aktif, hanya detail yang
                        // transaksinya di rentang itu yang dihitung; kalau
                        // tidak aktif, kondisi WHERE selalu benar.
                        bool pakaiFilter = filterDari.HasValue && filterSampai.HasValue;
                        cmd.Parameters.AddWithValue("@PakaiFilter", pakaiFilter ? 1 : 0);
                        cmd.Parameters.AddWithValue("@Dari",
                            pakaiFilter ? filterDari.Value.ToString("yyyy-MM-dd 00:00:00") : "");
                        cmd.Parameters.AddWithValue("@Sampai",
                            pakaiFilter ? filterSampai.Value.ToString("yyyy-MM-dd 23:59:59") : "");
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        DataTable result = new DataTable();
                        adapter.Fill(result);

                        decimal grandTotal = 0;
                        foreach (DataRow row in result.Rows)
                        {
                            grandTotal += Convert.ToDecimal(row["Total_Omzet"]);
                        }

                        laporanTable = result;
                        dgvLaporan.DataSource = laporanTable;
                        if (dgvLaporan.Columns["Total_Omzet"] != null)
                            dgvLaporan.Columns["Total_Omzet"].DefaultCellStyle.Format = "N0";
                        if (dgvLaporan.Columns["ID_Item"] != null)
                            dgvLaporan.Columns["ID_Item"].Visible = false;
                        lblTotalOmzet.Text = "Total Omzet: Rp " + grandTotal.ToString("N0");
                        TerapkanFilterLaporan();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat laporan:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
