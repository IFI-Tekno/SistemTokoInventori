namespace SistemTokoInventori
{
    partial class LaporanControl
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) { components.Dispose(); }
            base.Dispose(disposing);
        }
        private void InitializeComponent()
        {
            this.dgvLaporan = new Guna.UI2.WinForms.Guna2DataGridView();
            this.btnLoadLaporan = new Guna.UI2.WinForms.Guna2Button();
            this.pnlTotalOmzet = new Guna.UI2.WinForms.Guna2Panel();
            this.lblTotalOmzet = new System.Windows.Forms.Label();
            this.txtCari = new Guna.UI2.WinForms.Guna2TextBox();
            this.lblDari = new System.Windows.Forms.Label();
            this.dtpDari = new Guna.UI2.WinForms.Guna2DateTimePicker();
            this.lblSampai = new System.Windows.Forms.Label();
            this.dtpSampai = new Guna.UI2.WinForms.Guna2DateTimePicker();
            this.btnTerapkan = new Guna.UI2.WinForms.Guna2Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLaporan)).BeginInit();
            this.pnlTotalOmzet.SuspendLayout();
            this.SuspendLayout();
            // btnLoadLaporan
            this.btnLoadLaporan.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnLoadLaporan.Location = new System.Drawing.Point(20, 80);
            this.btnLoadLaporan.Name = "btnLoadLaporan";
            this.btnLoadLaporan.Size = new System.Drawing.Size(170, 42);
            this.btnLoadLaporan.TabIndex = 1;
            this.btnLoadLaporan.Text = "Load Laporan";
            this.btnLoadLaporan.Click += new System.EventHandler(this.btnLoadLaporan_Click);
            // txtCari
            this.txtCari.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtCari.Location = new System.Drawing.Point(206, 80);
            this.txtCari.Name = "txtCari";
            this.txtCari.PlaceholderText = "Cari produk...";
            this.txtCari.Size = new System.Drawing.Size(300, 42);
            this.txtCari.TabIndex = 2;
            this.txtCari.TextChanged += new System.EventHandler(this.txtCari_TextChanged);
            // lblDari
            this.lblDari.AutoSize = true;
            this.lblDari.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblDari.Location = new System.Drawing.Point(20, 141);
            this.lblDari.Name = "lblDari";
            this.lblDari.Size = new System.Drawing.Size(36, 19);
            this.lblDari.TabIndex = 4;
            this.lblDari.Text = "Dari";
            // dtpDari
            this.dtpDari.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.dtpDari.Location = new System.Drawing.Point(70, 132);
            this.dtpDari.Name = "dtpDari";
            this.dtpDari.Size = new System.Drawing.Size(150, 36);
            this.dtpDari.TabIndex = 5;
            // lblSampai
            this.lblSampai.AutoSize = true;
            this.lblSampai.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblSampai.Location = new System.Drawing.Point(236, 141);
            this.lblSampai.Name = "lblSampai";
            this.lblSampai.Size = new System.Drawing.Size(58, 19);
            this.lblSampai.TabIndex = 6;
            this.lblSampai.Text = "Sampai";
            // dtpSampai
            this.dtpSampai.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.dtpSampai.Location = new System.Drawing.Point(300, 132);
            this.dtpSampai.Name = "dtpSampai";
            this.dtpSampai.Size = new System.Drawing.Size(150, 36);
            this.dtpSampai.TabIndex = 7;
            // btnTerapkan
            this.btnTerapkan.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnTerapkan.Location = new System.Drawing.Point(466, 132);
            this.btnTerapkan.Name = "btnTerapkan";
            this.btnTerapkan.Size = new System.Drawing.Size(120, 36);
            this.btnTerapkan.TabIndex = 8;
            this.btnTerapkan.Text = "Terapkan";
            this.btnTerapkan.Click += new System.EventHandler(this.btnTerapkan_Click);
            // dgvLaporan
            this.dgvLaporan.AllowUserToAddRows = false;
            this.dgvLaporan.AllowUserToDeleteRows = false;
            this.dgvLaporan.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvLaporan.Location = new System.Drawing.Point(20, 184);
            this.dgvLaporan.Name = "dgvLaporan";
            this.dgvLaporan.ReadOnly = true;
            this.dgvLaporan.Size = new System.Drawing.Size(820, 254);
            this.dgvLaporan.TabIndex = 0;
            // pnlTotalOmzet
            this.pnlTotalOmzet.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.pnlTotalOmzet.Controls.Add(this.lblTotalOmzet);
            this.pnlTotalOmzet.Location = new System.Drawing.Point(20, 452);
            this.pnlTotalOmzet.Name = "pnlTotalOmzet";
            this.pnlTotalOmzet.Size = new System.Drawing.Size(420, 60);
            this.pnlTotalOmzet.TabIndex = 3;
            // lblTotalOmzet
            this.lblTotalOmzet.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTotalOmzet.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTotalOmzet.Location = new System.Drawing.Point(0, 0);
            this.lblTotalOmzet.Name = "lblTotalOmzet";
            this.lblTotalOmzet.Size = new System.Drawing.Size(420, 60);
            this.lblTotalOmzet.TabIndex = 2;
            this.lblTotalOmzet.Text = "Total Omzet: Rp 0";
            this.lblTotalOmzet.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // LaporanControl
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlTotalOmzet);
            this.Controls.Add(this.btnLoadLaporan);
            this.Controls.Add(this.txtCari);
            this.Controls.Add(this.lblDari);
            this.Controls.Add(this.dtpDari);
            this.Controls.Add(this.lblSampai);
            this.Controls.Add(this.dtpSampai);
            this.Controls.Add(this.btnTerapkan);
            this.Controls.Add(this.dgvLaporan);
            this.Name = "LaporanControl";
            this.Size = new System.Drawing.Size(860, 560);
            ((System.ComponentModel.ISupportInitialize)(this.dgvLaporan)).EndInit();
            this.pnlTotalOmzet.ResumeLayout(false);
            this.ResumeLayout(false);
        }
        private Guna.UI2.WinForms.Guna2DataGridView dgvLaporan;
        private Guna.UI2.WinForms.Guna2Button btnLoadLaporan;
        private Guna.UI2.WinForms.Guna2Panel pnlTotalOmzet;
        private System.Windows.Forms.Label lblTotalOmzet;
        private Guna.UI2.WinForms.Guna2TextBox txtCari;
        private System.Windows.Forms.Label lblDari;
        private Guna.UI2.WinForms.Guna2DateTimePicker dtpDari;
        private System.Windows.Forms.Label lblSampai;
        private Guna.UI2.WinForms.Guna2DateTimePicker dtpSampai;
        private Guna.UI2.WinForms.Guna2Button btnTerapkan;
    }
}
