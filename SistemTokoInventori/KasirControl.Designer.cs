namespace SistemTokoInventori
{
    partial class KasirControl
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
            this.dgvProduk = new Guna.UI2.WinForms.Guna2DataGridView();
            this.numQty = new Guna.UI2.WinForms.Guna2NumericUpDown();
            this.btnTambahCart = new Guna.UI2.WinForms.Guna2Button();
            this.dgvCart = new Guna.UI2.WinForms.Guna2DataGridView();
            this.pnlTotal = new Guna.UI2.WinForms.Guna2Panel();
            this.lblTotal = new System.Windows.Forms.Label();
            this.btnProses = new Guna.UI2.WinForms.Guna2Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lblQty = new System.Windows.Forms.Label();
            this.txtCari = new Guna.UI2.WinForms.Guna2TextBox();
            this.lblPelanggan = new System.Windows.Forms.Label();
            this.cmbPelanggan = new Guna.UI2.WinForms.Guna2ComboBox();
            this.lblPoinSaldo = new System.Windows.Forms.Label();
            this.lblPoinDipakai = new System.Windows.Forms.Label();
            this.numPoinDipakai = new Guna.UI2.WinForms.Guna2NumericUpDown();
            this.lblDepositSaldo = new System.Windows.Forms.Label();
            this.lblDepositDipakai = new System.Windows.Forms.Label();
            this.numDepositDipakai = new Guna.UI2.WinForms.Guna2NumericUpDown();
            this.lblDiskonAktif = new System.Windows.Forms.Label();
            this.lblRingkasan = new System.Windows.Forms.Label();
            this.btnTukarTambah = new Guna.UI2.WinForms.Guna2Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvProduk)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCart)).BeginInit();
            this.pnlTotal.SuspendLayout();
            this.SuspendLayout();
            //
            // lblPelanggan
            //
            this.lblPelanggan.AutoSize = true;
            this.lblPelanggan.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblPelanggan.Location = new System.Drawing.Point(20, 80);
            this.lblPelanggan.Name = "lblPelanggan";
            this.lblPelanggan.Size = new System.Drawing.Size(75, 19);
            this.lblPelanggan.TabIndex = 9;
            this.lblPelanggan.Text = "Pelanggan:";
            //
            // cmbPelanggan
            //
            this.cmbPelanggan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPelanggan.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cmbPelanggan.Location = new System.Drawing.Point(100, 76);
            this.cmbPelanggan.Name = "cmbPelanggan";
            this.cmbPelanggan.Size = new System.Drawing.Size(260, 36);
            this.cmbPelanggan.TabIndex = 10;
            this.cmbPelanggan.SelectedIndexChanged += new System.EventHandler(this.cmbPelanggan_SelectedIndexChanged);
            //
            // lblPoinSaldo
            //
            this.lblPoinSaldo.AutoSize = true;
            this.lblPoinSaldo.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblPoinSaldo.Location = new System.Drawing.Point(380, 80);
            this.lblPoinSaldo.Name = "lblPoinSaldo";
            this.lblPoinSaldo.Size = new System.Drawing.Size(48, 19);
            this.lblPoinSaldo.TabIndex = 11;
            this.lblPoinSaldo.Text = "Poin: 0";
            //
            // lblPoinDipakai
            //
            this.lblPoinDipakai.AutoSize = true;
            this.lblPoinDipakai.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblPoinDipakai.Location = new System.Drawing.Point(500, 80);
            this.lblPoinDipakai.Name = "lblPoinDipakai";
            this.lblPoinDipakai.Size = new System.Drawing.Size(95, 19);
            this.lblPoinDipakai.TabIndex = 12;
            this.lblPoinDipakai.Text = "Poin dipakai:";
            //
            // numPoinDipakai
            //
            this.numPoinDipakai.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.numPoinDipakai.Location = new System.Drawing.Point(600, 76);
            this.numPoinDipakai.Maximum = new decimal(new int[] { 0, 0, 0, 0 });
            this.numPoinDipakai.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.numPoinDipakai.Name = "numPoinDipakai";
            this.numPoinDipakai.Size = new System.Drawing.Size(80, 36);
            this.numPoinDipakai.TabIndex = 13;
            this.numPoinDipakai.Value = new decimal(new int[] { 0, 0, 0, 0 });
            this.numPoinDipakai.ValueChanged += new System.EventHandler(this.numPoinDipakai_ValueChanged);
            //
            // lblDepositSaldo
            //
            this.lblDepositSaldo.AutoSize = true;
            this.lblDepositSaldo.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblDepositSaldo.Location = new System.Drawing.Point(20, 108);
            this.lblDepositSaldo.Name = "lblDepositSaldo";
            this.lblDepositSaldo.Size = new System.Drawing.Size(100, 19);
            this.lblDepositSaldo.TabIndex = 17;
            this.lblDepositSaldo.Text = "Deposit: Rp 0";
            //
            // lblDepositDipakai
            //
            this.lblDepositDipakai.AutoSize = true;
            this.lblDepositDipakai.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblDepositDipakai.Location = new System.Drawing.Point(220, 108);
            this.lblDepositDipakai.Name = "lblDepositDipakai";
            this.lblDepositDipakai.Size = new System.Drawing.Size(118, 19);
            this.lblDepositDipakai.TabIndex = 18;
            this.lblDepositDipakai.Text = "Deposit dipakai:";
            //
            // numDepositDipakai
            //
            this.numDepositDipakai.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.numDepositDipakai.Location = new System.Drawing.Point(344, 104);
            this.numDepositDipakai.Maximum = new decimal(new int[] { 0, 0, 0, 0 });
            this.numDepositDipakai.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.numDepositDipakai.Name = "numDepositDipakai";
            this.numDepositDipakai.Size = new System.Drawing.Size(140, 36);
            this.numDepositDipakai.TabIndex = 19;
            this.numDepositDipakai.ThousandsSeparator = true;
            this.numDepositDipakai.Value = new decimal(new int[] { 0, 0, 0, 0 });
            this.numDepositDipakai.ValueChanged += new System.EventHandler(this.numDepositDipakai_ValueChanged);
            //
            // lblDiskonAktif
            //
            this.lblDiskonAktif.AutoSize = true;
            this.lblDiskonAktif.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblDiskonAktif.Location = new System.Drawing.Point(500, 108);
            this.lblDiskonAktif.Name = "lblDiskonAktif";
            this.lblDiskonAktif.Size = new System.Drawing.Size(100, 19);
            this.lblDiskonAktif.TabIndex = 14;
            this.lblDiskonAktif.Text = "Diskon 0% aktif";
            this.lblDiskonAktif.Visible = false;
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.label1.Location = new System.Drawing.Point(20, 124);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(110, 20);
            this.label1.TabIndex = 6;
            this.label1.Text = "Daftar Produk";
            //
            // txtCari
            //
            this.txtCari.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtCari.Location = new System.Drawing.Point(20, 148);
            this.txtCari.Name = "txtCari";
            this.txtCari.PlaceholderText = "Cari produk...";
            this.txtCari.Size = new System.Drawing.Size(320, 36);
            this.txtCari.TabIndex = 0;
            this.txtCari.TextChanged += new System.EventHandler(this.txtCari_TextChanged);
            //
            // dgvProduk
            //
            this.dgvProduk.AllowUserToAddRows = false;
            this.dgvProduk.AllowUserToDeleteRows = false;
            this.dgvProduk.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvProduk.Location = new System.Drawing.Point(20, 192);
            this.dgvProduk.Name = "dgvProduk";
            this.dgvProduk.ReadOnly = true;
            this.dgvProduk.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvProduk.Size = new System.Drawing.Size(560, 140);
            this.dgvProduk.TabIndex = 0;
            //
            // lblQty
            //
            this.lblQty.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblQty.AutoSize = true;
            this.lblQty.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblQty.Location = new System.Drawing.Point(600, 124);
            this.lblQty.Name = "lblQty";
            this.lblQty.Size = new System.Drawing.Size(35, 19);
            this.lblQty.TabIndex = 6;
            this.lblQty.Text = "Qty";
            //
            // numQty
            //
            this.numQty.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.numQty.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.numQty.Location = new System.Drawing.Point(600, 148);
            this.numQty.Maximum = new decimal(new int[] { 99999, 0, 0, 0 });
            this.numQty.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numQty.Name = "numQty";
            this.numQty.Size = new System.Drawing.Size(100, 25);
            this.numQty.TabIndex = 1;
            this.numQty.Value = new decimal(new int[] { 1, 0, 0, 0 });
            //
            // btnTambahCart
            //
            this.btnTambahCart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnTambahCart.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnTambahCart.Location = new System.Drawing.Point(600, 192);
            this.btnTambahCart.Name = "btnTambahCart";
            this.btnTambahCart.Size = new System.Drawing.Size(140, 42);
            this.btnTambahCart.TabIndex = 2;
            this.btnTambahCart.Text = "Tambah";
            this.btnTambahCart.Click += new System.EventHandler(this.btnTambahCart_Click);
            //
            // label2
            //
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.label2.Location = new System.Drawing.Point(20, 344);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(230, 20);
            this.label2.TabIndex = 7;
            this.label2.Text = "Keranjang (klik 2x untuk hapus)";
            //
            // dgvCart
            //
            this.dgvCart.AllowUserToAddRows = false;
            this.dgvCart.AllowUserToDeleteRows = false;
            this.dgvCart.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvCart.Location = new System.Drawing.Point(20, 368);
            this.dgvCart.Name = "dgvCart";
            this.dgvCart.ReadOnly = true;
            this.dgvCart.Size = new System.Drawing.Size(860, 120);
            this.dgvCart.TabIndex = 3;
            this.dgvCart.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvCart_CellDoubleClick);
            //
            // lblRingkasan
            //
            this.lblRingkasan.AutoSize = false;
            this.lblRingkasan.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblRingkasan.Location = new System.Drawing.Point(20, 496);
            this.lblRingkasan.Name = "lblRingkasan";
            this.lblRingkasan.Size = new System.Drawing.Size(860, 20);
            this.lblRingkasan.TabIndex = 15;
            this.lblRingkasan.Text = "Subtotal: Rp 0";
            this.lblRingkasan.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // btnTukarTambah
            //
            this.btnTukarTambah.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnTukarTambah.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnTukarTambah.Location = new System.Drawing.Point(500, 520);
            this.btnTukarTambah.Name = "btnTukarTambah";
            this.btnTukarTambah.Size = new System.Drawing.Size(180, 60);
            this.btnTukarTambah.TabIndex = 16;
            this.btnTukarTambah.Text = "Tukar Tambah";
            this.btnTukarTambah.Click += new System.EventHandler(this.btnTukarTambah_Click);
            //
            // pnlTotal
            //
            this.pnlTotal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.pnlTotal.Controls.Add(this.lblTotal);
            this.pnlTotal.Location = new System.Drawing.Point(20, 520);
            this.pnlTotal.Name = "pnlTotal";
            this.pnlTotal.Size = new System.Drawing.Size(360, 60);
            this.pnlTotal.TabIndex = 8;
            //
            // lblTotal
            //
            this.lblTotal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTotal.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTotal.Location = new System.Drawing.Point(0, 0);
            this.lblTotal.Name = "lblTotal";
            this.lblTotal.Size = new System.Drawing.Size(360, 60);
            this.lblTotal.TabIndex = 4;
            this.lblTotal.Text = "Total: Rp 0";
            this.lblTotal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // btnProses
            //
            this.btnProses.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnProses.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnProses.Location = new System.Drawing.Point(700, 520);
            this.btnProses.Name = "btnProses";
            this.btnProses.Size = new System.Drawing.Size(180, 60);
            this.btnProses.TabIndex = 5;
            this.btnProses.Text = "Proses Transaksi";
            this.btnProses.Click += new System.EventHandler(this.btnProses_Click);
            //
            // KasirControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnProses);
            this.Controls.Add(this.btnTukarTambah);
            this.Controls.Add(this.lblRingkasan);
            this.Controls.Add(this.pnlTotal);
            this.Controls.Add(this.dgvCart);
            this.Controls.Add(this.btnTambahCart);
            this.Controls.Add(this.numQty);
            this.Controls.Add(this.lblQty);
            this.Controls.Add(this.txtCari);
            this.Controls.Add(this.dgvProduk);
            this.Controls.Add(this.lblPelanggan);
            this.Controls.Add(this.cmbPelanggan);
            this.Controls.Add(this.lblPoinSaldo);
            this.Controls.Add(this.lblPoinDipakai);
            this.Controls.Add(this.numPoinDipakai);
            this.Controls.Add(this.lblDepositSaldo);
            this.Controls.Add(this.lblDepositDipakai);
            this.Controls.Add(this.numDepositDipakai);
            this.Controls.Add(this.lblDiskonAktif);
            this.Name = "KasirControl";
            this.Size = new System.Drawing.Size(900, 620);
            ((System.ComponentModel.ISupportInitialize)(this.dgvProduk)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCart)).EndInit();
            this.pnlTotal.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private Guna.UI2.WinForms.Guna2DataGridView dgvProduk;
        private Guna.UI2.WinForms.Guna2NumericUpDown numQty;
        private Guna.UI2.WinForms.Guna2Button btnTambahCart;
        private Guna.UI2.WinForms.Guna2DataGridView dgvCart;
        private Guna.UI2.WinForms.Guna2Panel pnlTotal;
        private System.Windows.Forms.Label lblTotal;
        private Guna.UI2.WinForms.Guna2Button btnProses;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblQty;
        private Guna.UI2.WinForms.Guna2TextBox txtCari;
        private System.Windows.Forms.Label lblPelanggan;
        private Guna.UI2.WinForms.Guna2ComboBox cmbPelanggan;
        private System.Windows.Forms.Label lblPoinSaldo;
        private System.Windows.Forms.Label lblPoinDipakai;
        private Guna.UI2.WinForms.Guna2NumericUpDown numPoinDipakai;
        private System.Windows.Forms.Label lblDepositSaldo;
        private System.Windows.Forms.Label lblDepositDipakai;
        private Guna.UI2.WinForms.Guna2NumericUpDown numDepositDipakai;
        private System.Windows.Forms.Label lblDiskonAktif;
        private System.Windows.Forms.Label lblRingkasan;
        private Guna.UI2.WinForms.Guna2Button btnTukarTambah;
    }
}
