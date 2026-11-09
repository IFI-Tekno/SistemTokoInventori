using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace SistemTokoInventori
{
    /// <summary>
    /// Modal read-only "History Harga" (Fase 2). Dibuka dari tab Item di
    /// MasterDataControl untuk item yang dipilih. Menampilkan isi tabel
    /// HargaJualHistory (Tanggal, HargaLama -&gt; HargaBaru, Keterangan),
    /// terbaru dulu. Tabelnya sudah dicatat otomatis sejak Fase 1 setiap
    /// kali harga jual diubah.
    /// UI dibangun dari kode (tanpa Designer) seperti MasterDataControl.
    /// </summary>
    public partial class FormHistoryHarga : Form
    {
        // Disimpan di field supaya Guna2Elipse tidak di-GC.
        private Guna2Elipse formElipse;

        private Guna2DataGridView dgvHistory;
        private Label lblKosong;
        private Guna2Button btnTutup;

        private int idItem;
        private string namaItem;

        public FormHistoryHarga(int idItem, string namaItem)
        {
            this.idItem = idItem;
            this.namaItem = namaItem;
            InitializeComponent();
            TerapkanTema();
            this.Load += FormHistoryHarga_Load;
        }

        private void FormHistoryHarga_Load(object sender, EventArgs e)
        {
            this.Text = "History Harga";
            var header = Theme.BuildHeader("History Harga", namaItem);
            this.Controls.Add(header);
            header.BringToFront();
            LoadHistory();
        }

        private void LoadHistory()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT Tanggal, HargaLama, HargaBaru, Keterangan " +
                        "FROM HargaJualHistory WHERE ID_Item = @ID " +
                        "ORDER BY Tanggal DESC, ID_History DESC", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idItem);
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dgvHistory.DataSource = dt;
                        if (dgvHistory.Columns["Tanggal"] != null)
                            dgvHistory.Columns["Tanggal"].HeaderText = "Tanggal";
                        if (dgvHistory.Columns["HargaLama"] != null)
                        {
                            dgvHistory.Columns["HargaLama"].HeaderText = "Harga Lama";
                            dgvHistory.Columns["HargaLama"].DefaultCellStyle.Format = "N0";
                        }
                        if (dgvHistory.Columns["HargaBaru"] != null)
                        {
                            dgvHistory.Columns["HargaBaru"].HeaderText = "Harga Baru";
                            dgvHistory.Columns["HargaBaru"].DefaultCellStyle.Format = "N0";
                        }
                        if (dgvHistory.Columns["Keterangan"] != null)
                            dgvHistory.Columns["Keterangan"].HeaderText = "Keterangan";
                        lblKosong.Visible = dt.Rows.Count == 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat history harga:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComponent()
        {
            this.dgvHistory = new Guna2DataGridView();
            this.lblKosong = new Label();
            this.btnTutup = new Guna2Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).BeginInit();
            this.SuspendLayout();
            //
            // dgvHistory
            //
            this.dgvHistory.AllowUserToAddRows = false;
            this.dgvHistory.AllowUserToDeleteRows = false;
            this.dgvHistory.Location = new Point(20, 76);
            this.dgvHistory.Name = "dgvHistory";
            this.dgvHistory.ReadOnly = true;
            this.dgvHistory.Size = new Size(560, 300);
            this.dgvHistory.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            //
            // lblKosong
            //
            this.lblKosong.AutoSize = false;
            this.lblKosong.Font = new Font("Segoe UI", 10F);
            this.lblKosong.Location = new Point(20, 200);
            this.lblKosong.Name = "lblKosong";
            this.lblKosong.Size = new Size(560, 24);
            this.lblKosong.Text = "Belum ada perubahan harga untuk item ini.";
            this.lblKosong.TextAlign = ContentAlignment.MiddleCenter;
            this.lblKosong.Visible = false;
            //
            // btnTutup
            //
            this.btnTutup.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            this.btnTutup.Location = new Point(20, 392);
            this.btnTutup.Name = "btnTutup";
            this.btnTutup.Size = new Size(200, 46);
            this.btnTutup.Text = "Tutup";
            this.btnTutup.Click += new EventHandler(this.btnTutup_Click);
            //
            // FormHistoryHarga
            //
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(600, 458);
            this.Controls.Add(this.dgvHistory);
            this.Controls.Add(this.lblKosong);
            this.Controls.Add(this.btnTutup);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormHistoryHarga";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "History Harga";
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void TerapkanTema()
        {
            Theme.StyleForm(this);
            formElipse = Theme.ApplyRoundedForm(this);
            Theme.StyleGrid(dgvHistory);
            Theme.StylePrimaryButton(btnTutup);
            lblKosong.ForeColor = Theme.Muted;
        }

        private void btnTutup_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
