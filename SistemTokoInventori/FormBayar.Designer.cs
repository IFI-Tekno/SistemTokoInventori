namespace SistemTokoInventori
{
    partial class FormBayar
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
            this.lblTotal = new System.Windows.Forms.Label();
            this.rdoTunai = new System.Windows.Forms.RadioButton();
            this.rdoTempo = new System.Windows.Forms.RadioButton();
            this.lblJatuhTempo = new System.Windows.Forms.Label();
            this.dtpJatuhTempo = new Guna.UI2.WinForms.Guna2DateTimePicker();
            this.lblBayar = new System.Windows.Forms.Label();
            this.txtBayar = new Guna.UI2.WinForms.Guna2TextBox();
            this.lblKembalian = new System.Windows.Forms.Label();
            this.lblWarning = new System.Windows.Forms.Label();
            this.btnKonfirmasi = new Guna.UI2.WinForms.Guna2Button();
            this.btnBatal = new Guna.UI2.WinForms.Guna2Button();
            this.SuspendLayout();
            //
            // lblTotal
            //
            this.lblTotal.AutoSize = false;
            this.lblTotal.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTotal.Location = new System.Drawing.Point(20, 84);
            this.lblTotal.Name = "lblTotal";
            this.lblTotal.Size = new System.Drawing.Size(420, 36);
            this.lblTotal.TabIndex = 0;
            this.lblTotal.Text = "Total: Rp 0";
            //
            // rdoTunai
            //
            this.rdoTunai.AutoSize = true;
            this.rdoTunai.Checked = true;
            this.rdoTunai.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.rdoTunai.Location = new System.Drawing.Point(20, 128);
            this.rdoTunai.Name = "rdoTunai";
            this.rdoTunai.Size = new System.Drawing.Size(64, 23);
            this.rdoTunai.TabIndex = 1;
            this.rdoTunai.TabStop = true;
            this.rdoTunai.Text = "Tunai";
            this.rdoTunai.CheckedChanged += new System.EventHandler(this.rdoJenisBayar_CheckedChanged);
            //
            // rdoTempo
            //
            this.rdoTempo.AutoSize = true;
            this.rdoTempo.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.rdoTempo.Location = new System.Drawing.Point(150, 128);
            this.rdoTempo.Name = "rdoTempo";
            this.rdoTempo.Size = new System.Drawing.Size(70, 23);
            this.rdoTempo.TabIndex = 2;
            this.rdoTempo.Text = "Tempo";
            this.rdoTempo.CheckedChanged += new System.EventHandler(this.rdoJenisBayar_CheckedChanged);
            //
            // lblJatuhTempo
            //
            this.lblJatuhTempo.AutoSize = true;
            this.lblJatuhTempo.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblJatuhTempo.Location = new System.Drawing.Point(20, 160);
            this.lblJatuhTempo.Name = "lblJatuhTempo";
            this.lblJatuhTempo.Size = new System.Drawing.Size(95, 19);
            this.lblJatuhTempo.TabIndex = 3;
            this.lblJatuhTempo.Text = "Jatuh tempo:";
            this.lblJatuhTempo.Visible = false;
            //
            // dtpJatuhTempo
            //
            this.dtpJatuhTempo.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.dtpJatuhTempo.Location = new System.Drawing.Point(140, 156);
            this.dtpJatuhTempo.Name = "dtpJatuhTempo";
            this.dtpJatuhTempo.Size = new System.Drawing.Size(200, 30);
            this.dtpJatuhTempo.TabIndex = 4;
            this.dtpJatuhTempo.Visible = false;
            //
            // lblBayar
            //
            this.lblBayar.AutoSize = true;
            this.lblBayar.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblBayar.Location = new System.Drawing.Point(20, 196);
            this.lblBayar.Name = "lblBayar";
            this.lblBayar.Size = new System.Drawing.Size(140, 19);
            this.lblBayar.TabIndex = 5;
            this.lblBayar.Text = "Uang Diterima (Rp)";
            //
            // txtBayar
            //
            this.txtBayar.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.txtBayar.Location = new System.Drawing.Point(20, 220);
            this.txtBayar.Name = "txtBayar";
            this.txtBayar.PlaceholderText = "Contoh: 50000";
            this.txtBayar.Size = new System.Drawing.Size(420, 42);
            this.txtBayar.TabIndex = 6;
            this.txtBayar.TextChanged += new System.EventHandler(this.txtBayar_TextChanged);
            //
            // lblKembalian
            //
            this.lblKembalian.AutoSize = false;
            this.lblKembalian.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblKembalian.Location = new System.Drawing.Point(20, 278);
            this.lblKembalian.Name = "lblKembalian";
            this.lblKembalian.Size = new System.Drawing.Size(420, 36);
            this.lblKembalian.TabIndex = 7;
            this.lblKembalian.Text = "Kembalian: Rp 0";
            //
            // lblWarning
            //
            this.lblWarning.AutoSize = false;
            this.lblWarning.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblWarning.Location = new System.Drawing.Point(20, 320);
            this.lblWarning.Name = "lblWarning";
            this.lblWarning.Size = new System.Drawing.Size(420, 24);
            this.lblWarning.TabIndex = 8;
            this.lblWarning.Text = "Uang kurang!";
            this.lblWarning.Visible = false;
            //
            // btnKonfirmasi
            //
            this.btnKonfirmasi.Enabled = false;
            this.btnKonfirmasi.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnKonfirmasi.Location = new System.Drawing.Point(20, 360);
            this.btnKonfirmasi.Name = "btnKonfirmasi";
            this.btnKonfirmasi.Size = new System.Drawing.Size(200, 46);
            this.btnKonfirmasi.TabIndex = 9;
            this.btnKonfirmasi.Text = "Konfirmasi Bayar";
            this.btnKonfirmasi.Click += new System.EventHandler(this.btnKonfirmasi_Click);
            //
            // btnBatal
            //
            this.btnBatal.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnBatal.Location = new System.Drawing.Point(240, 360);
            this.btnBatal.Name = "btnBatal";
            this.btnBatal.Size = new System.Drawing.Size(200, 46);
            this.btnBatal.TabIndex = 10;
            this.btnBatal.Text = "Batal";
            this.btnBatal.Click += new System.EventHandler(this.btnBatal_Click);
            //
            // FormBayar
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(460, 436);
            this.Controls.Add(this.lblTotal);
            this.Controls.Add(this.rdoTunai);
            this.Controls.Add(this.rdoTempo);
            this.Controls.Add(this.lblJatuhTempo);
            this.Controls.Add(this.dtpJatuhTempo);
            this.Controls.Add(this.lblBayar);
            this.Controls.Add(this.txtBayar);
            this.Controls.Add(this.lblKembalian);
            this.Controls.Add(this.lblWarning);
            this.Controls.Add(this.btnKonfirmasi);
            this.Controls.Add(this.btnBatal);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormBayar";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Pembayaran";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTotal;
        private System.Windows.Forms.RadioButton rdoTunai;
        private System.Windows.Forms.RadioButton rdoTempo;
        private System.Windows.Forms.Label lblJatuhTempo;
        private Guna.UI2.WinForms.Guna2DateTimePicker dtpJatuhTempo;
        private System.Windows.Forms.Label lblBayar;
        private Guna.UI2.WinForms.Guna2TextBox txtBayar;
        private System.Windows.Forms.Label lblKembalian;
        private System.Windows.Forms.Label lblWarning;
        private Guna.UI2.WinForms.Guna2Button btnKonfirmasi;
        private Guna.UI2.WinForms.Guna2Button btnBatal;
    }
}
