namespace SistemTokoInventori
{
    partial class FormStruk
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
            this.txtStruk = new Guna.UI2.WinForms.Guna2TextBox();
            this.btnTutup = new Guna.UI2.WinForms.Guna2Button();
            this.SuspendLayout();
            //
            // txtStruk
            //
            this.txtStruk.Font = new System.Drawing.Font("Consolas", 10F);
            this.txtStruk.Location = new System.Drawing.Point(20, 84);
            this.txtStruk.Multiline = true;
            this.txtStruk.Name = "txtStruk";
            this.txtStruk.ReadOnly = true;
            this.txtStruk.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtStruk.Size = new System.Drawing.Size(420, 400);
            this.txtStruk.TabIndex = 0;
            //
            // btnTutup
            //
            this.btnTutup.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnTutup.Location = new System.Drawing.Point(130, 500);
            this.btnTutup.Name = "btnTutup";
            this.btnTutup.Size = new System.Drawing.Size(200, 46);
            this.btnTutup.TabIndex = 1;
            this.btnTutup.Text = "Tutup";
            this.btnTutup.Click += new System.EventHandler(this.btnTutup_Click);
            //
            // FormStruk
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(460, 560);
            this.Controls.Add(this.txtStruk);
            this.Controls.Add(this.btnTutup);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormStruk";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Struk";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private Guna.UI2.WinForms.Guna2TextBox txtStruk;
        private Guna.UI2.WinForms.Guna2Button btnTutup;
    }
}
