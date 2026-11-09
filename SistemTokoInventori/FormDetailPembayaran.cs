using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace SistemTokoInventori
{
    /// <summary>
    /// Modal read-only "Detail Pembayaran" generik (Fase 2) — dipakai tab
    /// Hutang (PembelianControl) dan tab Piutang (PenjualanControl) untuk
    /// menampilkan riwayat pembayaran satu tagihan: tanggal, jumlah,
    /// keterangan, plus ringkasan total / sudah dibayar / sisa.
    /// UI dibangun dari kode (tanpa Designer) seperti MasterDataControl.
    /// </summary>
    public partial class FormDetailPembayaran : Form
    {
        // Disimpan di field supaya Guna2Elipse tidak di-GC.
        private Guna2Elipse formElipse;

        private Label lblRingkasan;
        private Guna2DataGridView dgvRiwayat;
        private Label lblKosong;
        private Guna2Button btnTutup;

        // Judul dialog + konteks — diisi pemanggil sebelum ShowDialog().
        public string Judul { get; set; }
        public string Konteks { get; set; }
        public decimal Total { get; set; }
        public decimal Sisa { get; set; }

        // Riwayat pembayaran: DataTable dengan kolom Tanggal, Jumlah,
        // Keterangan. Diisi pemanggil sebelum ShowDialog().
        public DataTable Pembayaran { get; set; }

        public FormDetailPembayaran()
        {
            InitializeComponent();
            TerapkanTema();
            this.Load += FormDetailPembayaran_Load;
        }

        private void FormDetailPembayaran_Load(object sender, EventArgs e)
        {
            this.Text = string.IsNullOrEmpty(Judul) ? "Detail Pembayaran" : Judul;
            var header = Theme.BuildHeader(this.Text, Konteks ?? "");
            this.Controls.Add(header);
            header.BringToFront();
            decimal dibayar = Total - Sisa;
            lblRingkasan.Text = "Total: Rp " + Total.ToString("N0") +
                "   •   Sudah dibayar: Rp " + dibayar.ToString("N0") +
                "   •   Sisa: Rp " + Sisa.ToString("N0");
            if (Pembayaran != null)
            {
                dgvRiwayat.DataSource = Pembayaran;
                if (dgvRiwayat.Columns["Tanggal"] != null)
                    dgvRiwayat.Columns["Tanggal"].HeaderText = "Tanggal";
                if (dgvRiwayat.Columns["Jumlah"] != null)
                {
                    dgvRiwayat.Columns["Jumlah"].HeaderText = "Jumlah";
                    dgvRiwayat.Columns["Jumlah"].DefaultCellStyle.Format = "N0";
                }
                if (dgvRiwayat.Columns["Keterangan"] != null)
                    dgvRiwayat.Columns["Keterangan"].HeaderText = "Keterangan";
                lblKosong.Visible = Pembayaran.Rows.Count == 0;
            }
        }

        private void InitializeComponent()
        {
            this.lblRingkasan = new Label();
            this.dgvRiwayat = new Guna2DataGridView();
            this.lblKosong = new Label();
            this.btnTutup = new Guna2Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRiwayat)).BeginInit();
            this.SuspendLayout();
            //
            // lblRingkasan
            //
            this.lblRingkasan.AutoSize = false;
            this.lblRingkasan.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblRingkasan.Location = new Point(20, 76);
            this.lblRingkasan.Name = "lblRingkasan";
            this.lblRingkasan.Size = new Size(560, 20);
            //
            // dgvRiwayat
            //
            this.dgvRiwayat.AllowUserToAddRows = false;
            this.dgvRiwayat.AllowUserToDeleteRows = false;
            this.dgvRiwayat.Location = new Point(20, 104);
            this.dgvRiwayat.Name = "dgvRiwayat";
            this.dgvRiwayat.ReadOnly = true;
            this.dgvRiwayat.Size = new Size(560, 280);
            this.dgvRiwayat.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            //
            // lblKosong
            //
            this.lblKosong.AutoSize = false;
            this.lblKosong.Font = new Font("Segoe UI", 10F);
            this.lblKosong.Location = new Point(20, 200);
            this.lblKosong.Name = "lblKosong";
            this.lblKosong.Size = new Size(560, 24);
            this.lblKosong.Text = "Belum ada pembayaran untuk tagihan ini.";
            this.lblKosong.TextAlign = ContentAlignment.MiddleCenter;
            this.lblKosong.Visible = false;
            //
            // btnTutup
            //
            this.btnTutup.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            this.btnTutup.Location = new Point(20, 400);
            this.btnTutup.Name = "btnTutup";
            this.btnTutup.Size = new Size(200, 46);
            this.btnTutup.Text = "Tutup";
            this.btnTutup.Click += new EventHandler(this.btnTutup_Click);
            //
            // FormDetailPembayaran
            //
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(600, 466);
            this.Controls.Add(this.lblRingkasan);
            this.Controls.Add(this.dgvRiwayat);
            this.Controls.Add(this.lblKosong);
            this.Controls.Add(this.btnTutup);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormDetailPembayaran";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Detail Pembayaran";
            ((System.ComponentModel.ISupportInitialize)(this.dgvRiwayat)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void TerapkanTema()
        {
            Theme.StyleForm(this);
            formElipse = Theme.ApplyRoundedForm(this);
            Theme.StyleGrid(dgvRiwayat);
            Theme.StylePrimaryButton(btnTutup);
            lblRingkasan.ForeColor = Theme.Text;
            lblKosong.ForeColor = Theme.Muted;
        }

        private void btnTutup_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
