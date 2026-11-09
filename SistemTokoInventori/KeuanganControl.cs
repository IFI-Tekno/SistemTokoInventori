using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace SistemTokoInventori
{
    /// <summary>
    /// Modul Keuangan &amp; Akuntansi (Fase 4): Jurnal Umum (otomatis dari
    /// JurnalHelper — Kasir, PO, hutang/piutang, konsinyasi, opname,
    /// biaya, deposit), Buku Besar, Deposit pelanggan, Biaya operasional,
    /// Neraca, Laba Rugi (Bulanan/YTD), dan editor Saldo Awal akun.
    /// UI dibangun dari kode (tanpa Designer) seperti PembelianControl.
    ///
    /// Aturan penting (lihat V2_NOTES §10):
    /// - Jurnal dimulai dari go-live Fase 4 — transaksi Fase 1-3 TIDAK
    ///   di-backfill. Neraca &amp; Buku Besar = SaldoAwal akun + jurnal.
    /// - Neraca seimbang (Selisih 0) kalau SaldoAwal seimbang DAN semua
    ///   jurnal seimbang (dijamin JurnalHelper).
    /// - Laba Berjalan di Neraca = pendapatan - HPP - biaya sejak 1 Jan
    ///   sampai akhir periode yang dipilih.
    /// - Biaya operasional TIDAK bisa dihapus (hanya tambah) — supaya
    ///   jurnal tidak pernah kehilangan pasangan debit/kreditnya.
    /// </summary>
    public partial class KeuanganControl : UserControl
    {
        private Guna2TabControl tabControl;
        private TabPage tabJurnal;
        private TabPage tabBukuBesar;
        private TabPage tabDeposit;
        private TabPage tabBiaya;
        private TabPage tabNeraca;
        private TabPage tabLabaRugi;
        private TabPage tabSaldoAwal;

        // ----- Jurnal Umum -----
        private Guna2DateTimePicker dtpDariJurnal;
        private Guna2DateTimePicker dtpSampaiJurnal;
        private Guna2ComboBox cmbAkunJurnal;
        private Guna2Button btnTampilJurnal;
        private Guna2Button btnCetakJurnal;
        private Guna2DataGridView dgvJurnal;
        private Label lblTotalJurnal;
        private DataTable jurnalTable;

        // ----- Buku Besar -----
        private Guna2ComboBox cmbAkunBB;
        private Guna2DateTimePicker dtpDariBB;
        private Guna2DateTimePicker dtpSampaiBB;
        private Guna2Button btnTampilBB;
        private Label lblSaldoAwalBB;
        private Guna2DataGridView dgvBB;
        private Label lblSaldoAkhirBB;
        private DataTable bbTable;

        // ----- Deposit -----
        private Guna2DataGridView dgvDeposit;
        private Guna2ComboBox cmbPelangganDep;
        private Guna2NumericUpDown numJumlahDep;
        private Guna2TextBox txtKetDep;
        private Guna2Button btnTerimaDeposit;
        private Guna2Button btnKembalikanDeposit;
        private Guna2Button btnDetailDeposit;
        private DataTable depositTable;

        // ----- Biaya -----
        private Guna2DataGridView dgvBiaya;
        private Guna2ComboBox cmbAkunBiaya;
        private Guna2DateTimePicker dtpTanggalBiaya;
        private Guna2NumericUpDown numJumlahBiaya;
        private Guna2TextBox txtKetBiaya;
        private Guna2Button btnTambahBiaya;
        private DataTable biayaTable;

        // ----- Neraca -----
        private Guna2DateTimePicker dtpPeriodeNeraca;
        private Guna2Button btnTampilNeraca;
        private Guna2Button btnCetakNeraca;
        private Guna2DataGridView dgvAktiva;
        private Guna2DataGridView dgvPasiva;
        private Label lblTotalAktiva;
        private Label lblTotalPasiva;
        private Label lblSelisihNeraca;
        private DataTable neracaAktivaTable;
        private DataTable neracaPasivaTable;
        private decimal totalAktivaCetak;
        private decimal totalPasivaCetak;

        // ----- Laba Rugi -----
        private Guna2DateTimePicker dtpPeriodeLR;
        private RadioButton rdoBulanan;
        private RadioButton rdoYTD;
        private Guna2Button btnTampilLR;
        private Guna2Button btnCetakLR;
        private Guna2DataGridView dgvLR;
        private Label lblLabaBersih;
        private DataTable lrTable;

        // ----- Saldo Awal -----
        private Guna2DataGridView dgvSaldoAwal;
        private Label lblAkunSA;
        private Guna2NumericUpDown numSaldoAwal;
        private Guna2Button btnSimpanSA;
        private DataTable saldoAwalTable;
        private int? idAkunSA;

        // ----- Cetak (satu printer teks generik untuk Jurnal/Neraca/LR) -----
        private List<string> printBaris;
        private int printIndex;
        private PrintDocument docCetak;

        // Akun neraca (urut tampil) + akun biaya (urut tampil di Laba Rugi).
        private static readonly string[] AkunAktiva = { "1101", "1102", "1103", "1104", "1201" };
        private static readonly string[] AkunPasiva = { "2101", "2102", "2103", "3101", "3201" };
        private static readonly string[] AkunBiaya = { "6101", "6102", "6103", "6104", "6105", "6106" };

        public KeuanganControl()
        {
            InitializeComponent();
            TerapkanTema();
            LoadAkunJurnalCombo();
            LoadAkunBBCombo();
            LoadPelangganDepCombo();
            LoadAkunBiayaCombo();
            LoadJurnal();
            LoadDeposit();
            LoadBiaya();
            LoadSaldoAwal();
            TampilNeraca();
            TampilLabaRugi();
        }

        /// <summary>Dipanggil FormMain setiap kali tab Keuangan dibuka.</summary>
        public void RefreshData()
        {
            LoadAkunJurnalCombo();
            LoadAkunBBCombo();
            LoadPelangganDepCombo();
            LoadAkunBiayaCombo();
            LoadJurnal();
            LoadDeposit();
            LoadBiaya();
            LoadSaldoAwal();
            TampilNeraca();
            TampilLabaRugi();
        }

        private void TerapkanTema()
        {
            this.BackColor = Theme.Background;
            this.Font = Theme.Font(10);
            Theme.StyleTabControl(tabControl);
            Theme.StyleGrid(dgvJurnal);
            Theme.StyleGrid(dgvBB);
            Theme.StyleGrid(dgvDeposit);
            Theme.StyleGrid(dgvBiaya);
            Theme.StyleGrid(dgvAktiva);
            Theme.StyleGrid(dgvPasiva);
            Theme.StyleGrid(dgvLR);
            Theme.StyleGrid(dgvSaldoAwal);
            Theme.StyleDatePicker(dtpDariJurnal);
            Theme.StyleDatePicker(dtpSampaiJurnal);
            Theme.StyleDatePicker(dtpDariBB);
            Theme.StyleDatePicker(dtpSampaiBB);
            Theme.StyleDatePicker(dtpTanggalBiaya);
            // Picker bulan Neraca/Laba Rugi: beri gaya yang sama, lalu
            // kembalikan format bulannya (StyleDatePicker memaksa Short).
            Theme.StyleDatePicker(dtpPeriodeNeraca);
            Theme.StyleDatePicker(dtpPeriodeLR);
            dtpPeriodeNeraca.Format = DateTimePickerFormat.Custom;
            dtpPeriodeNeraca.CustomFormat = "MMMM yyyy";
            dtpPeriodeNeraca.ShowUpDown = true;
            dtpPeriodeLR.Format = DateTimePickerFormat.Custom;
            dtpPeriodeLR.CustomFormat = "MMMM yyyy";
            dtpPeriodeLR.ShowUpDown = true;
            Theme.StyleComboBox(cmbAkunJurnal);
            Theme.StyleComboBox(cmbAkunBB);
            Theme.StyleComboBox(cmbPelangganDep);
            Theme.StyleComboBox(cmbAkunBiaya);
            Theme.StyleNumericUpDown(numJumlahDep);
            Theme.StyleNumericUpDown(numJumlahBiaya);
            Theme.StyleNumericUpDown(numSaldoAwal);
            Theme.StyleTextBox(txtKetDep);
            Theme.StyleTextBox(txtKetBiaya);
            Theme.StylePrimaryButton(btnTampilJurnal);
            Theme.StylePrimaryButton(btnTampilBB);
            Theme.StylePrimaryButton(btnTampilNeraca);
            Theme.StylePrimaryButton(btnTampilLR);
            Theme.StyleSuccessButton(btnTerimaDeposit);
            Theme.StyleSuccessButton(btnTambahBiaya);
            Theme.StyleSuccessButton(btnSimpanSA);
            Theme.StyleGhostButton(btnCetakJurnal);
            Theme.StyleGhostButton(btnCetakNeraca);
            Theme.StyleGhostButton(btnCetakLR);
            Theme.StyleGhostButton(btnKembalikanDeposit);
            Theme.StyleGhostButton(btnDetailDeposit);
            lblTotalJurnal.ForeColor = Theme.Text;
            lblSaldoAwalBB.ForeColor = Theme.Text;
            lblSaldoAkhirBB.ForeColor = Theme.Text;
            lblTotalAktiva.ForeColor = Theme.Text;
            lblTotalPasiva.ForeColor = Theme.Text;
            lblLabaBersih.ForeColor = Theme.SuccessDark;
        }

        // ==================== PEMBANGUN UI ====================
        private void InitializeComponent()
        {
            this.tabControl = new Guna.UI2.WinForms.Guna2TabControl();
            this.tabJurnal = new System.Windows.Forms.TabPage();
            this.tabBukuBesar = new System.Windows.Forms.TabPage();
            this.tabDeposit = new System.Windows.Forms.TabPage();
            this.tabBiaya = new System.Windows.Forms.TabPage();
            this.tabNeraca = new System.Windows.Forms.TabPage();
            this.tabLabaRugi = new System.Windows.Forms.TabPage();
            this.tabSaldoAwal = new System.Windows.Forms.TabPage();
            this.tabControl.Controls.Add(this.tabJurnal);
            this.tabControl.Controls.Add(this.tabBukuBesar);
            this.tabControl.Controls.Add(this.tabDeposit);
            this.tabControl.Controls.Add(this.tabBiaya);
            this.tabControl.Controls.Add(this.tabNeraca);
            this.tabControl.Controls.Add(this.tabLabaRugi);
            this.tabControl.Controls.Add(this.tabSaldoAwal);
            this.tabControl.Dock = DockStyle.Fill;
            this.tabControl.Name = "tabControl";
            this.tabJurnal.Text = "Jurnal Umum";
            this.tabBukuBesar.Text = "Buku Besar";
            this.tabDeposit.Text = "Deposit";
            this.tabBiaya.Text = "Biaya";
            this.tabNeraca.Text = "Neraca";
            this.tabLabaRugi.Text = "Laba Rugi";
            this.tabSaldoAwal.Text = "Saldo Awal";

            BangunTabJurnal();
            BangunTabBukuBesar();
            BangunTabDeposit();
            BangunTabBiaya();
            BangunTabNeraca();
            BangunTabLabaRugi();
            BangunTabSaldoAwal();

            this.Controls.Add(this.tabControl);
            this.Name = "KeuanganControl";
            this.Size = new System.Drawing.Size(900, 620);
            var header = Theme.BuildHeader("Keuangan",
                "Jurnal, buku besar, deposit, biaya, neraca, dan laba rugi");
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

        private Label BuatLabelTebal(string teks, int x, int y)
        {
            Label lbl = BuatLabel(teks, x, y);
            lbl.Font = Theme.Font(10, FontStyle.Bold);
            return lbl;
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

        private Guna2DateTimePicker BuatDatePicker(string nama, int x, int y, DateTime nilai)
        {
            Guna2DateTimePicker dtp = new Guna2DateTimePicker();
            dtp.Name = nama;
            dtp.Font = Theme.Font(10);
            dtp.Location = new Point(x, y);
            dtp.Size = new Size(130, 30);
            dtp.Value = nilai;
            return dtp;
        }

        // DateTimePicker bulan saja (MMMM yyyy, tombol naik/turun) untuk
        // periode Neraca / Laba Rugi — harinya diabaikan.
        private Guna2DateTimePicker BuatBulanPicker(string nama, int x, int y)
        {
            Guna2DateTimePicker dtp = BuatDatePicker(nama, x, y, DateTime.Now);
            dtp.Size = new Size(170, 30);
            dtp.Format = DateTimePickerFormat.Custom;
            dtp.CustomFormat = "MMMM yyyy";
            dtp.ShowUpDown = true;
            return dtp;
        }

        private Guna2NumericUpDown BuatUang(string nama, int x, int y, int lebar)
        {
            Guna2NumericUpDown num = new Guna2NumericUpDown();
            num.Name = nama;
            num.Font = Theme.Font(10);
            num.Location = new Point(x, y);
            num.Size = new Size(lebar, 30);
            num.Minimum = 0;
            num.Maximum = 1000000000m;
            num.ThousandsSeparator = true;
            return num;
        }

        private void BangunTabJurnal()
        {
            tabJurnal.Controls.Add(BuatLabel("Dari:", 12, 20));
            dtpDariJurnal = BuatDatePicker("dtpDariJurnal", 58, 14,
                new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1));
            tabJurnal.Controls.Add(dtpDariJurnal);
            tabJurnal.Controls.Add(BuatLabel("Sampai:", 198, 20));
            dtpSampaiJurnal = BuatDatePicker("dtpSampaiJurnal", 258, 14, DateTime.Now);
            tabJurnal.Controls.Add(dtpSampaiJurnal);
            tabJurnal.Controls.Add(BuatLabel("Akun:", 398, 20));
            cmbAkunJurnal = new Guna2ComboBox();
            cmbAkunJurnal.Name = "cmbAkunJurnal";
            cmbAkunJurnal.Font = Theme.Font(10);
            cmbAkunJurnal.Location = new Point(444, 14);
            cmbAkunJurnal.Size = new Size(210, 30);
            tabJurnal.Controls.Add(cmbAkunJurnal);
            btnTampilJurnal = BuatButton("btnTampilJurnal", "Tampilkan", 664, 10, btnTampilJurnal_Click);
            btnTampilJurnal.Size = new Size(100, 36);
            tabJurnal.Controls.Add(btnTampilJurnal);
            btnCetakJurnal = BuatButton("btnCetakJurnal", "Cetak Jurnal", 774, 10, btnCetakJurnal_Click);
            btnCetakJurnal.Size = new Size(110, 36);
            tabJurnal.Controls.Add(btnCetakJurnal);
            dgvJurnal = BuatGrid("dgvJurnal", 380);
            tabJurnal.Controls.Add(dgvJurnal);
            lblTotalJurnal = BuatLabelTebal("Total Debit: Rp 0  •  Total Kredit: Rp 0", 12, 448);
            tabJurnal.Controls.Add(lblTotalJurnal);
        }

        private void BangunTabBukuBesar()
        {
            tabBukuBesar.Controls.Add(BuatLabel("Akun:", 12, 20));
            cmbAkunBB = new Guna2ComboBox();
            cmbAkunBB.Name = "cmbAkunBB";
            cmbAkunBB.Font = Theme.Font(10);
            cmbAkunBB.Location = new Point(58, 14);
            cmbAkunBB.Size = new Size(260, 30);
            tabBukuBesar.Controls.Add(cmbAkunBB);
            tabBukuBesar.Controls.Add(BuatLabel("Dari:", 328, 20));
            dtpDariBB = BuatDatePicker("dtpDariBB", 374, 14,
                new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1));
            tabBukuBesar.Controls.Add(dtpDariBB);
            tabBukuBesar.Controls.Add(BuatLabel("Sampai:", 514, 20));
            dtpSampaiBB = BuatDatePicker("dtpSampaiBB", 574, 14, DateTime.Now);
            tabBukuBesar.Controls.Add(dtpSampaiBB);
            btnTampilBB = BuatButton("btnTampilBB", "Tampilkan", 714, 10, btnTampilBB_Click);
            btnTampilBB.Size = new Size(100, 36);
            tabBukuBesar.Controls.Add(btnTampilBB);
            lblSaldoAwalBB = BuatLabel("Saldo Awal: Rp 0", 12, 56);
            tabBukuBesar.Controls.Add(lblSaldoAwalBB);
            dgvBB = BuatGrid("dgvBB", 340);
            dgvBB.Location = new Point(12, 84);
            tabBukuBesar.Controls.Add(dgvBB);
            lblSaldoAkhirBB = BuatLabelTebal("Saldo Akhir: Rp 0", 12, 436);
            tabBukuBesar.Controls.Add(lblSaldoAkhirBB);
        }

        private void BangunTabDeposit()
        {
            dgvDeposit = BuatGrid("dgvDeposit", 180);
            dgvDeposit.CellClick += dgvDeposit_CellClick;
            tabDeposit.Controls.Add(dgvDeposit);
            tabDeposit.Controls.Add(BuatLabelTebal("Terima Deposit", 12, 252));
            tabDeposit.Controls.Add(BuatLabel("Pelanggan:", 12, 282));
            cmbPelangganDep = new Guna2ComboBox();
            cmbPelangganDep.Name = "cmbPelangganDep";
            cmbPelangganDep.Font = Theme.Font(10);
            cmbPelangganDep.Location = new Point(100, 276);
            cmbPelangganDep.Size = new Size(240, 30);
            tabDeposit.Controls.Add(cmbPelangganDep);
            tabDeposit.Controls.Add(BuatLabel("Jumlah:", 356, 282));
            numJumlahDep = BuatUang("numJumlahDep", 420, 276, 150);
            tabDeposit.Controls.Add(numJumlahDep);
            tabDeposit.Controls.Add(BuatLabel("Keterangan:", 586, 282));
            txtKetDep = new Guna2TextBox();
            txtKetDep.Name = "txtKetDep";
            txtKetDep.Font = Theme.Font(10);
            txtKetDep.Location = new Point(676, 276);
            txtKetDep.Size = new Size(200, 30);
            txtKetDep.PlaceholderText = "Mis. DP pesanan";
            tabDeposit.Controls.Add(txtKetDep);
            btnTerimaDeposit = BuatButton("btnTerimaDeposit", "Terima Deposit", 12, 318, btnTerimaDeposit_Click);
            btnTerimaDeposit.Size = new Size(160, 42);
            tabDeposit.Controls.Add(btnTerimaDeposit);
            btnKembalikanDeposit = BuatButton("btnKembalikanDeposit", "Kembalikan", 182, 318, btnKembalikanDeposit_Click);
            btnKembalikanDeposit.Size = new Size(140, 42);
            tabDeposit.Controls.Add(btnKembalikanDeposit);
            btnDetailDeposit = BuatButton("btnDetailDeposit", "Riwayat Pakai", 332, 318, btnDetailDeposit_Click);
            btnDetailDeposit.Size = new Size(140, 42);
            tabDeposit.Controls.Add(btnDetailDeposit);
            Label lblCatatan = BuatLabel("Deposit adalah uang muka pelanggan (kewajiban, bukan pendapatan). " +
                "Dipakai otomatis di Kasir kalau pelanggan dipilih — deposit tertua dulu.", 12, 374);
            lblCatatan.ForeColor = Theme.Muted;
            tabDeposit.Controls.Add(lblCatatan);
        }

        private void BangunTabBiaya()
        {
            dgvBiaya = BuatGrid("dgvBiaya", 220);
            tabBiaya.Controls.Add(dgvBiaya);
            tabBiaya.Controls.Add(BuatLabelTebal("Tambah Biaya Operasional", 12, 292));
            tabBiaya.Controls.Add(BuatLabel("Akun:", 12, 322));
            cmbAkunBiaya = new Guna2ComboBox();
            cmbAkunBiaya.Name = "cmbAkunBiaya";
            cmbAkunBiaya.Font = Theme.Font(10);
            cmbAkunBiaya.Location = new Point(60, 316);
            cmbAkunBiaya.Size = new Size(240, 30);
            tabBiaya.Controls.Add(cmbAkunBiaya);
            tabBiaya.Controls.Add(BuatLabel("Tanggal:", 316, 322));
            dtpTanggalBiaya = BuatDatePicker("dtpTanggalBiaya", 384, 316, DateTime.Now);
            tabBiaya.Controls.Add(dtpTanggalBiaya);
            tabBiaya.Controls.Add(BuatLabel("Jumlah:", 530, 322));
            numJumlahBiaya = BuatUang("numJumlahBiaya", 596, 316, 150);
            tabBiaya.Controls.Add(numJumlahBiaya);
            tabBiaya.Controls.Add(BuatLabel("Keterangan:", 12, 362));
            txtKetBiaya = new Guna2TextBox();
            txtKetBiaya.Name = "txtKetBiaya";
            txtKetBiaya.Font = Theme.Font(10);
            txtKetBiaya.Location = new Point(100, 356);
            txtKetBiaya.Size = new Size(400, 30);
            txtKetBiaya.PlaceholderText = "Mis. Gaji karyawan November";
            tabBiaya.Controls.Add(txtKetBiaya);
            btnTambahBiaya = BuatButton("btnTambahBiaya", "Tambah Biaya", 520, 352, btnTambahBiaya_Click);
            btnTambahBiaya.Size = new Size(150, 42);
            tabBiaya.Controls.Add(btnTambahBiaya);
            Label lblCatatan = BuatLabel("Biaya TIDAK bisa dihapus — kalau salah input, catat biaya koreksi " +
                "dengan keterangan yang jelas (supaya jurnalnya tetap utuh).", 12, 408);
            lblCatatan.ForeColor = Theme.Muted;
            tabBiaya.Controls.Add(lblCatatan);
        }

        private void BangunTabNeraca()
        {
            tabNeraca.Controls.Add(BuatLabel("Periode:", 12, 20));
            dtpPeriodeNeraca = BuatBulanPicker("dtpPeriodeNeraca", 80, 14);
            tabNeraca.Controls.Add(dtpPeriodeNeraca);
            btnTampilNeraca = BuatButton("btnTampilNeraca", "Tampilkan", 260, 10, btnTampilNeraca_Click);
            btnTampilNeraca.Size = new Size(110, 36);
            tabNeraca.Controls.Add(btnTampilNeraca);
            btnCetakNeraca = BuatButton("btnCetakNeraca", "Cetak Neraca", 380, 10, btnCetakNeraca_Click);
            btnCetakNeraca.Size = new Size(110, 36);
            tabNeraca.Controls.Add(btnCetakNeraca);
            dgvAktiva = BuatGrid("dgvAktiva", 330);
            dgvAktiva.Size = new Size(410, 330);
            tabNeraca.Controls.Add(dgvAktiva);
            dgvPasiva = BuatGrid("dgvPasiva", 330);
            dgvPasiva.Location = new Point(442, 56);
            dgvPasiva.Size = new Size(410, 330);
            tabNeraca.Controls.Add(dgvPasiva);
            lblTotalAktiva = BuatLabelTebal("Total Aktiva: Rp 0", 12, 398);
            tabNeraca.Controls.Add(lblTotalAktiva);
            lblTotalPasiva = BuatLabelTebal("Total Pasiva: Rp 0", 442, 398);
            tabNeraca.Controls.Add(lblTotalPasiva);
            lblSelisihNeraca = BuatLabelTebal("Selisih: Rp 0", 12, 428);
            tabNeraca.Controls.Add(lblSelisihNeraca);
            Label lblCatatan = BuatLabel("Neraca memakai SaldoAwal akun + jurnal sampai akhir periode. " +
                "Laba Berjalan = pendapatan - HPP - biaya sejak 1 Januari.", 12, 458);
            lblCatatan.ForeColor = Theme.Muted;
            tabNeraca.Controls.Add(lblCatatan);
        }

        private void BangunTabLabaRugi()
        {
            tabLabaRugi.Controls.Add(BuatLabel("Periode:", 12, 20));
            dtpPeriodeLR = BuatBulanPicker("dtpPeriodeLR", 80, 14);
            tabLabaRugi.Controls.Add(dtpPeriodeLR);
            rdoBulanan = new RadioButton();
            rdoBulanan.Name = "rdoBulanan";
            rdoBulanan.Text = "Bulanan";
            rdoBulanan.Checked = true;
            rdoBulanan.AutoSize = true;
            rdoBulanan.Font = Theme.Font(10, FontStyle.Bold);
            rdoBulanan.Location = new Point(270, 18);
            tabLabaRugi.Controls.Add(rdoBulanan);
            rdoYTD = new RadioButton();
            rdoYTD.Name = "rdoYTD";
            rdoYTD.Text = "YTD";
            rdoYTD.AutoSize = true;
            rdoYTD.Font = Theme.Font(10, FontStyle.Bold);
            rdoYTD.Location = new Point(360, 18);
            tabLabaRugi.Controls.Add(rdoYTD);
            btnTampilLR = BuatButton("btnTampilLR", "Tampilkan", 440, 10, btnTampilLR_Click);
            btnTampilLR.Size = new Size(110, 36);
            tabLabaRugi.Controls.Add(btnTampilLR);
            btnCetakLR = BuatButton("btnCetakLR", "Cetak Laba Rugi", 560, 10, btnCetakLR_Click);
            btnCetakLR.Size = new Size(130, 36);
            tabLabaRugi.Controls.Add(btnCetakLR);
            dgvLR = BuatGrid("dgvLR", 360);
            dgvLR.Size = new Size(640, 360);
            tabLabaRugi.Controls.Add(dgvLR);
            lblLabaBersih = BuatLabelTebal("Laba Bersih: Rp 0", 12, 428);
            tabLabaRugi.Controls.Add(lblLabaBersih);
        }

        private void BangunTabSaldoAwal()
        {
            dgvSaldoAwal = BuatGrid("dgvSaldoAwal", 300);
            dgvSaldoAwal.CellClick += dgvSaldoAwal_CellClick;
            tabSaldoAwal.Controls.Add(dgvSaldoAwal);
            lblAkunSA = BuatLabel("Pilih akun di grid dulu.", 12, 374);
            tabSaldoAwal.Controls.Add(lblAkunSA);
            tabSaldoAwal.Controls.Add(BuatLabel("Saldo Awal:", 12, 404));
            numSaldoAwal = BuatUang("numSaldoAwal", 100, 398, 180);
            tabSaldoAwal.Controls.Add(numSaldoAwal);
            btnSimpanSA = BuatButton("btnSimpanSA", "Simpan Saldo Awal", 300, 394, btnSimpanSA_Click);
            btnSimpanSA.Size = new Size(170, 42);
            tabSaldoAwal.Controls.Add(btnSimpanSA);
            Label lblCatatan = BuatLabel("Saldo awal dipakai Neraca & Buku Besar. Pastikan total Aktiva = total " +
                "Kewajiban + Modal (neraca awal seimbang) — kalau tidak, Neraca menampilkan selisih.", 12, 448);
            lblCatatan.ForeColor = Theme.Muted;
            tabSaldoAwal.Controls.Add(lblCatatan);
        }

        // ==================== COMBO LOADERS ====================
        // Aturan hotfix Kasir: DisplayMember/ValueMember SEBELUM DataSource.
        private void LoadAkunJurnalCombo()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("ID_Akun", typeof(int));
            dt.Columns.Add("NamaTampil", typeof(string));
            dt.Rows.Add(0, "Semua Akun");
            using (SQLiteConnection conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT ID_Akun, Kode || ' - ' || Nama AS NamaTampil " +
                    "FROM Akun WHERE IsAktif = 1 ORDER BY Kode", conn))
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                    while (reader.Read())
                        dt.Rows.Add(Convert.ToInt32(reader[0]), Convert.ToString(reader[1]));
            }
            cmbAkunJurnal.DisplayMember = "NamaTampil";
            cmbAkunJurnal.ValueMember = "ID_Akun";
            cmbAkunJurnal.DataSource = dt;
        }

        private void LoadAkunBBCombo()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("ID_Akun", typeof(int));
            dt.Columns.Add("NamaTampil", typeof(string));
            using (SQLiteConnection conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT ID_Akun, Kode || ' - ' || Nama AS NamaTampil " +
                    "FROM Akun WHERE IsAktif = 1 ORDER BY Kode", conn))
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                    while (reader.Read())
                        dt.Rows.Add(Convert.ToInt32(reader[0]), Convert.ToString(reader[1]));
            }
            cmbAkunBB.DisplayMember = "NamaTampil";
            cmbAkunBB.ValueMember = "ID_Akun";
            cmbAkunBB.DataSource = dt;
        }

        private void LoadPelangganDepCombo()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("ID_Pelanggan", typeof(int));
            dt.Columns.Add("Nama", typeof(string));
            using (SQLiteConnection conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT ID_Pelanggan, Nama FROM Pelanggan ORDER BY Nama", conn))
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                    while (reader.Read())
                        dt.Rows.Add(Convert.ToInt32(reader[0]), Convert.ToString(reader[1]));
            }
            cmbPelangganDep.DisplayMember = "Nama";
            cmbPelangganDep.ValueMember = "ID_Pelanggan";
            cmbPelangganDep.DataSource = dt;
        }

        private void LoadAkunBiayaCombo()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("ID_Akun", typeof(int));
            dt.Columns.Add("NamaTampil", typeof(string));
            using (SQLiteConnection conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT ID_Akun, Kode || ' - ' || Nama AS NamaTampil " +
                    "FROM Akun WHERE Jenis = 'Biaya' AND IsAktif = 1 " +
                    "ORDER BY Kode", conn))
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                    while (reader.Read())
                        dt.Rows.Add(Convert.ToInt32(reader[0]), Convert.ToString(reader[1]));
            }
            cmbAkunBiaya.DisplayMember = "NamaTampil";
            cmbAkunBiaya.ValueMember = "ID_Akun";
            cmbAkunBiaya.DataSource = dt;
        }

        private string AmbilKodeAkunTerpilih(Guna2ComboBox combo)
        {
            if (combo.SelectedValue == null) return null;
            if (combo.SelectedValue is DataRowView baris)
                return null; // belum sempat mengikat; abaikan
            int id = Convert.ToInt32(combo.SelectedValue);
            if (id == 0) return null;
            using (SQLiteConnection conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT Kode FROM Akun WHERE ID_Akun = @ID", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    object hasil = cmd.ExecuteScalar();
                    return hasil == null ? null : hasil.ToString();
                }
            }
        }

        private string Rp(decimal nilai)
        {
            return "Rp " + nilai.ToString("N0", CultureInfo.InvariantCulture);
        }

        // ==================== JURNAL UMUM ====================
        private void btnTampilJurnal_Click(object sender, EventArgs e)
        {
            LoadJurnal();
        }

        private void LoadJurnal()
        {
            string dari = dtpDariJurnal.Value.Date.ToString("yyyy-MM-dd");
            string sampai = dtpSampaiJurnal.Value.Date.ToString("yyyy-MM-dd");
            int idAkun = (cmbAkunJurnal.SelectedValue == null ||
                cmbAkunJurnal.SelectedValue is DataRowView)
                ? 0 : Convert.ToInt32(cmbAkunJurnal.SelectedValue);
            using (SQLiteConnection conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                string sql =
                    "SELECT j.Tanggal, j.NoBukti, j.Keterangan, " +
                    "a.Kode || ' - ' || a.Nama AS Akun, d.Debit, d.Kredit " +
                    "FROM JurnalDetail d " +
                    "INNER JOIN Jurnal j ON j.ID_Jurnal = d.ID_Jurnal " +
                    "INNER JOIN Akun a ON a.ID_Akun = d.ID_Akun " +
                    "WHERE date(j.Tanggal) BETWEEN @Dari AND @Sampai ";
                if (idAkun != 0) sql += "AND a.ID_Akun = @ID_Akun ";
                sql += "ORDER BY j.Tanggal, j.ID_Jurnal, d.ID_JurnalDetail";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Dari", dari);
                    cmd.Parameters.AddWithValue("@Sampai", sampai);
                    if (idAkun != 0) cmd.Parameters.AddWithValue("@ID_Akun", idAkun);
                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                    {
                        jurnalTable = new DataTable();
                        adapter.Fill(jurnalTable);
                    }
                }
            }
            dgvJurnal.DataSource = jurnalTable;
            if (dgvJurnal.Columns.Contains("Tanggal")) dgvJurnal.Columns["Tanggal"].Width = 140;
            if (dgvJurnal.Columns.Contains("NoBukti")) dgvJurnal.Columns["NoBukti"].Width = 90;
            if (dgvJurnal.Columns.Contains("Keterangan")) dgvJurnal.Columns["Keterangan"].Width = 200;
            if (dgvJurnal.Columns.Contains("Akun")) dgvJurnal.Columns["Akun"].Width = 200;
            if (dgvJurnal.Columns.Contains("Debit")) dgvJurnal.Columns["Debit"].DefaultCellStyle.Format = "N0";
            if (dgvJurnal.Columns.Contains("Kredit")) dgvJurnal.Columns["Kredit"].DefaultCellStyle.Format = "N0";
            decimal totalDebit = 0;
            decimal totalKredit = 0;
            foreach (DataRow baris in jurnalTable.Rows)
            {
                totalDebit += Convert.ToDecimal(baris["Debit"]);
                totalKredit += Convert.ToDecimal(baris["Kredit"]);
            }
            bool seimbang = totalDebit == totalKredit;
            lblTotalJurnal.Text = "Total Debit: " + Rp(totalDebit) + "  •  Total Kredit: " +
                Rp(totalKredit) + "  •  " + (seimbang ? "Seimbang" : "TIDAK SEIMBANG");
            lblTotalJurnal.ForeColor = seimbang ? Theme.SuccessDark : Theme.DangerDark;
        }

        private void btnCetakJurnal_Click(object sender, EventArgs e)
        {
            List<string> baris = new List<string>();
            baris.Add("**JURNAL UMUM");
            baris.Add("Periode: " + dtpDariJurnal.Value.Date.ToString("dd MMM yyyy") +
                " - " + dtpSampaiJurnal.Value.Date.ToString("dd MMM yyyy"));
            baris.Add("");
            foreach (DataRow row in jurnalTable.Rows)
            {
                baris.Add(row["Tanggal"] + "  |  " + row["NoBukti"] + "  |  " +
                    row["Keterangan"] + "  |  " + row["Akun"] + "  |  D " +
                    Convert.ToDecimal(row["Debit"]).ToString("N0") + "  |  K " +
                    Convert.ToDecimal(row["Kredit"]).ToString("N0"));
            }
            baris.Add("");
            baris.Add("**" + lblTotalJurnal.Text);
            CetakTeks("Jurnal Umum", baris);
        }

        // ==================== BUKU BESAR ====================
        private void btnTampilBB_Click(object sender, EventArgs e)
        {
            TampilBB();
        }

        private void TampilBB()
        {
            string kode = AmbilKodeAkunTerpilih(cmbAkunBB);
            if (kode == null) return;
            string dari = dtpDariBB.Value.Date.ToString("yyyy-MM-dd");
            string sampai = dtpSampaiBB.Value.Date.ToString("yyyy-MM-dd");
            // Saldo awal = SaldoAwal akun + semua gerakan SEBELUM periode
            // (sampai akhir hari sebelum "Dari" — gerakan di tanggal "Dari"
            // jam 00:00:00 tetap masuk periode, bukan saldo awal).
            string sebelumPeriode = dtpDariBB.Value.Date.AddDays(-1)
                .ToString("yyyy-MM-dd 23:59:59");
            using (SQLiteConnection conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                string saldoNormal;
                using (var cmd = new SQLiteCommand(
                    "SELECT SaldoNormal FROM Akun WHERE Kode = @Kode", conn))
                {
                    cmd.Parameters.AddWithValue("@Kode", kode);
                    saldoNormal = Convert.ToString(cmd.ExecuteScalar());
                }
                decimal saldoAwal = JurnalHelper.SaldoAkunSampai(conn, null, kode, sebelumPeriode);
                lblSaldoAwalBB.Text = "Saldo Awal (" + dari + "): " + Rp(saldoAwal) +
                    "  (" + saldoNormal + "-normal)";
                DataTable gerak = new DataTable();
                gerak.Columns.Add("Tanggal", typeof(string));
                gerak.Columns.Add("NoBukti", typeof(string));
                gerak.Columns.Add("Keterangan", typeof(string));
                gerak.Columns.Add("Debit", typeof(decimal));
                gerak.Columns.Add("Kredit", typeof(decimal));
                gerak.Columns.Add("Saldo", typeof(decimal));
                using (var cmd = new SQLiteCommand(
                    "SELECT j.Tanggal, j.NoBukti, j.Keterangan, d.Debit, d.Kredit " +
                    "FROM JurnalDetail d " +
                    "INNER JOIN Jurnal j ON j.ID_Jurnal = d.ID_Jurnal " +
                    "INNER JOIN Akun a ON a.ID_Akun = d.ID_Akun " +
                    "WHERE a.Kode = @Kode " +
                    "AND j.Tanggal >= @Awal AND j.Tanggal <= @Akhir " +
                    "ORDER BY j.Tanggal, j.ID_Jurnal, d.ID_JurnalDetail", conn))
                {
                    cmd.Parameters.AddWithValue("@Kode", kode);
                    cmd.Parameters.AddWithValue("@Awal", dari + " 00:00:00");
                    cmd.Parameters.AddWithValue("@Akhir", sampai + " 23:59:59");
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        decimal saldoBerjalan = saldoAwal;
                        while (reader.Read())
                        {
                            decimal debit = Convert.ToDecimal(reader["Debit"]);
                            decimal kredit = Convert.ToDecimal(reader["Kredit"]);
                            if (saldoNormal == "Debit")
                                saldoBerjalan += debit - kredit;
                            else
                                saldoBerjalan += kredit - debit;
                            gerak.Rows.Add(Convert.ToString(reader["Tanggal"]),
                                Convert.ToString(reader["NoBukti"]),
                                Convert.ToString(reader["Keterangan"]),
                                debit, kredit, saldoBerjalan);
                        }
                        lblSaldoAkhirBB.Text = "Saldo Akhir (" + sampai + "): " +
                            Rp(saldoBerjalan);
                        lblSaldoAkhirBB.ForeColor =
                            saldoBerjalan >= 0 ? Theme.Text : Theme.DangerDark;
                    }
                }
                bbTable = gerak;
                dgvBB.DataSource = bbTable;
                if (dgvBB.Columns.Contains("Tanggal")) dgvBB.Columns["Tanggal"].Width = 140;
                if (dgvBB.Columns.Contains("NoBukti")) dgvBB.Columns["NoBukti"].Width = 90;
                if (dgvBB.Columns.Contains("Keterangan")) dgvBB.Columns["Keterangan"].Width = 200;
                if (dgvBB.Columns.Contains("Debit")) dgvBB.Columns["Debit"].DefaultCellStyle.Format = "N0";
                if (dgvBB.Columns.Contains("Kredit")) dgvBB.Columns["Kredit"].DefaultCellStyle.Format = "N0";
                if (dgvBB.Columns.Contains("Saldo")) dgvBB.Columns["Saldo"].DefaultCellStyle.Format = "N0";
            }
        }

        // ==================== CETAK (printer teks generik) ====================
        private void CetakTeks(string judulDokumen, List<string> baris)
        {
            printBaris = baris;
            printIndex = 0;
            docCetak = new PrintDocument();
            docCetak.DocumentName = judulDokumen;
            docCetak.PrintPage += DocCetak_PrintPage;
            PrintPreviewDialog preview = new PrintPreviewDialog();
            preview.Document = docCetak;
            preview.ShowDialog();
        }

        private void DocCetak_PrintPage(object sender, PrintPageEventArgs e)
        {
            Font biasa = new Font("Segoe UI", 10);
            Font tebal = new Font("Segoe UI", 10, FontStyle.Bold);
            float x = e.MarginBounds.Left;
            float y = e.MarginBounds.Top;
            while (printIndex < printBaris.Count)
            {
                string baris = printBaris[printIndex];
                bool cetakTebal = baris.StartsWith("**");
                string teks = cetakTebal ? baris.Substring(2) : baris;
                e.Graphics.DrawString(teks, cetakTebal ? tebal : biasa,
                    Brushes.Black, x, y);
                y += 20;
                printIndex++;
                if (y > e.MarginBounds.Bottom - 40)
                {
                    e.HasMorePages = true;
                    return;
                }
            }
            e.HasMorePages = false;
        }

        // ==================== DEPOSIT ====================
        private void LoadDeposit()
        {
            using (SQLiteConnection conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT d.ID_Deposit, p.Nama AS Pelanggan, d.Tanggal, " +
                    "d.Jumlah, d.Sisa, d.Status " +
                    "FROM Deposit d INNER JOIN Pelanggan p " +
                    "ON p.ID_Pelanggan = d.ID_Pelanggan " +
                    "ORDER BY d.ID_Deposit DESC", conn))
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                {
                    depositTable = new DataTable();
                    adapter.Fill(depositTable);
                }
            }
            dgvDeposit.DataSource = depositTable;
            if (dgvDeposit.Columns.Contains("ID_Deposit")) dgvDeposit.Columns["ID_Deposit"].Visible = false;
            if (dgvDeposit.Columns.Contains("Pelanggan")) dgvDeposit.Columns["Pelanggan"].Width = 200;
            if (dgvDeposit.Columns.Contains("Tanggal")) dgvDeposit.Columns["Tanggal"].Width = 140;
            if (dgvDeposit.Columns.Contains("Jumlah")) dgvDeposit.Columns["Jumlah"].DefaultCellStyle.Format = "N0";
            if (dgvDeposit.Columns.Contains("Sisa")) dgvDeposit.Columns["Sisa"].DefaultCellStyle.Format = "N0";
        }

        private int? IdDepositTerpilih()
        {
            if (dgvDeposit.SelectedRows.Count == 0) return null;
            return Convert.ToInt32(dgvDeposit.SelectedRows[0].Cells["ID_Deposit"].Value);
        }

        private void dgvDeposit_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Pilihan baris dipakai tombol Kembalikan & Riwayat Pakai.
        }

        private void btnTerimaDeposit_Click(object sender, EventArgs e)
        {
            if (cmbPelangganDep.SelectedValue == null ||
                cmbPelangganDep.SelectedValue is DataRowView)
            {
                MessageBox.Show("Pilih pelanggan dulu.");
                return;
            }
            if (numJumlahDep.Value <= 0)
            {
                MessageBox.Show("Jumlah deposit harus lebih dari 0.");
                return;
            }
            int idPelanggan = Convert.ToInt32(cmbPelangganDep.SelectedValue);
            decimal jumlah = numJumlahDep.Value;
            string tanggal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string keterangan = txtKetDep.Text.Trim();
            using (SQLiteConnection conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (SQLiteTransaction trans = conn.BeginTransaction())
                {
                    try
                    {
                        long idDeposit;
                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO Deposit (ID_Pelanggan, Tanggal, Jumlah, " +
                            "Sisa, Status, Keterangan) " +
                            "VALUES (@ID_Pelanggan, @Tanggal, @Jumlah, @Jumlah, " +
                            "'Aktif', @Keterangan)", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID_Pelanggan", idPelanggan);
                            cmd.Parameters.AddWithValue("@Tanggal", tanggal);
                            cmd.Parameters.AddWithValue("@Jumlah", jumlah);
                            cmd.Parameters.AddWithValue("@Keterangan", keterangan);
                            cmd.ExecuteNonQuery();
                            idDeposit = conn.LastInsertRowId;
                        }
                        // Jurnal: Debit Kas [jumlah]; Kredit Deposit Pelanggan.
                        JurnalHelper.TulisJurnalDepositTerima(conn, trans,
                            tanggal, jumlah, idDeposit);
                        trans.Commit();
                        MessageBox.Show("Deposit " + Rp(jumlah) + " diterima.");
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        MessageBox.Show("Gagal menerima deposit: " + ex.Message);
                        return;
                    }
                }
            }
            numJumlahDep.Value = 0;
            txtKetDep.Clear();
            LoadDeposit();
        }

        private void btnKembalikanDeposit_Click(object sender, EventArgs e)
        {
            int? idDeposit = IdDepositTerpilih();
            if (idDeposit == null)
            {
                MessageBox.Show("Pilih deposit di grid dulu.");
                return;
            }
            string namaPelanggan = dgvDeposit.SelectedRows[0].Cells["Pelanggan"].Value.ToString();
            decimal sisa = Convert.ToDecimal(dgvDeposit.SelectedRows[0].Cells["Sisa"].Value);
            if (sisa <= 0)
            {
                MessageBox.Show("Deposit ini sudah tidak ada sisa.");
                return;
            }
            FormCatatPembayaran form = new FormCatatPembayaran();
            form.Judul = "Kembalikan Deposit";
            form.Konteks = "Pelanggan: " + namaPelanggan;
            form.Sisa = sisa;
            if (form.ShowDialog() != DialogResult.OK) return;
            using (SQLiteConnection conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (SQLiteTransaction trans = conn.BeginTransaction())
                {
                    try
                    {
                        // Penjaga di SQL: Sisa >= jumlah, supaya dua refund
                        // bersamaan tidak bisa mengembalikan lebih dari Sisa.
                        using (var cmd = new SQLiteCommand(
                            "UPDATE Deposit SET Sisa = Sisa - @Jumlah, " +
                            "Status = CASE WHEN Sisa - @Jumlah <= 0 " +
                            "THEN 'Dikembalikan' ELSE 'Aktif' END " +
                            "WHERE ID_Deposit = @ID AND Sisa >= @Jumlah", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Jumlah", form.Jumlah);
                            cmd.Parameters.AddWithValue("@ID", idDeposit.Value);
                            if (cmd.ExecuteNonQuery() == 0)
                            {
                                trans.Rollback();
                                MessageBox.Show("Pengembalian gagal — sisa deposit sudah berubah.");
                                return;
                            }
                        }
                        // Jurnal: Debit Deposit Pelanggan; Kredit Kas.
                        JurnalHelper.TulisJurnalDepositKembali(conn, trans,
                            form.Tanggal.ToString("yyyy-MM-dd HH:mm:ss"),
                            form.Jumlah, idDeposit.Value);
                        trans.Commit();
                        MessageBox.Show("Deposit " + Rp(form.Jumlah) + " dikembalikan.");
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        MessageBox.Show("Gagal mengembalikan deposit: " + ex.Message);
                        return;
                    }
                }
            }
            LoadDeposit();
        }

        private void btnDetailDeposit_Click(object sender, EventArgs e)
        {
            int? idDeposit = IdDepositTerpilih();
            if (idDeposit == null)
            {
                MessageBox.Show("Pilih deposit di grid dulu.");
                return;
            }
            string namaPelanggan = dgvDeposit.SelectedRows[0].Cells["Pelanggan"].Value.ToString();
            decimal jumlah = Convert.ToDecimal(dgvDeposit.SelectedRows[0].Cells["Jumlah"].Value);
            decimal sisa = Convert.ToDecimal(dgvDeposit.SelectedRows[0].Cells["Sisa"].Value);
            DataTable riwayat = new DataTable();
            using (SQLiteConnection conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT dp.Tanggal, dp.Jumlah, " +
                    "'INV-' || dp.ID_Transaksi AS Keterangan " +
                    "FROM DepositPakai dp WHERE dp.ID_Deposit = @ID " +
                    "ORDER BY dp.Tanggal DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", idDeposit.Value);
                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                        adapter.Fill(riwayat);
                }
            }
            FormDetailPembayaran form = new FormDetailPembayaran();
            form.Judul = "Riwayat Pakai Deposit";
            form.Konteks = "Pelanggan: " + namaPelanggan;
            form.Total = jumlah;
            form.Sisa = sisa;
            form.Pembayaran = riwayat;
            form.ShowDialog();
        }

        // ==================== BIAYA OPERASIONAL ====================
        private void LoadBiaya()
        {
            using (SQLiteConnection conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT b.ID_Biaya, b.Tanggal, " +
                    "a.Kode || ' - ' || a.Nama AS Akun, b.Jumlah, " +
                    "b.Keterangan, b.NoBukti " +
                    "FROM BiayaOperasional b INNER JOIN Akun a " +
                    "ON a.ID_Akun = b.ID_Akun ORDER BY b.ID_Biaya DESC", conn))
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                {
                    biayaTable = new DataTable();
                    adapter.Fill(biayaTable);
                }
            }
            dgvBiaya.DataSource = biayaTable;
            if (dgvBiaya.Columns.Contains("ID_Biaya")) dgvBiaya.Columns["ID_Biaya"].Visible = false;
            if (dgvBiaya.Columns.Contains("Tanggal")) dgvBiaya.Columns["Tanggal"].Width = 140;
            if (dgvBiaya.Columns.Contains("Akun")) dgvBiaya.Columns["Akun"].Width = 220;
            if (dgvBiaya.Columns.Contains("Jumlah")) dgvBiaya.Columns["Jumlah"].DefaultCellStyle.Format = "N0";
            if (dgvBiaya.Columns.Contains("Keterangan")) dgvBiaya.Columns["Keterangan"].Width = 220;
        }

        private void btnTambahBiaya_Click(object sender, EventArgs e)
        {
            if (cmbAkunBiaya.SelectedValue == null ||
                cmbAkunBiaya.SelectedValue is DataRowView)
            {
                MessageBox.Show("Pilih akun biaya dulu.");
                return;
            }
            if (numJumlahBiaya.Value <= 0)
            {
                MessageBox.Show("Jumlah biaya harus lebih dari 0.");
                return;
            }
            string kode = AmbilKodeAkunTerpilih(cmbAkunBiaya);
            if (kode == null) return;
            decimal jumlah = numJumlahBiaya.Value;
            string tanggal = dtpTanggalBiaya.Value.ToString("yyyy-MM-dd HH:mm:ss");
            string keterangan = txtKetBiaya.Text.Trim();
            using (SQLiteConnection conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (SQLiteTransaction trans = conn.BeginTransaction())
                {
                    try
                    {
                        int idAkun;
                        using (var cmd = new SQLiteCommand(
                            "SELECT ID_Akun FROM Akun WHERE Kode = @Kode", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Kode", kode);
                            idAkun = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        long idBiaya;
                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO BiayaOperasional (Tanggal, ID_Akun, " +
                            "Jumlah, Keterangan, NoBukti) " +
                            "VALUES (@Tanggal, @ID_Akun, @Jumlah, @Keterangan, '')",
                            conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Tanggal", tanggal);
                            cmd.Parameters.AddWithValue("@ID_Akun", idAkun);
                            cmd.Parameters.AddWithValue("@Jumlah", jumlah);
                            cmd.Parameters.AddWithValue("@Keterangan", keterangan);
                            cmd.ExecuteNonQuery();
                            idBiaya = conn.LastInsertRowId;
                        }
                        string noBukti = "BIA-" + idBiaya;
                        using (var cmd = new SQLiteCommand(
                            "UPDATE BiayaOperasional SET NoBukti = @NoBukti " +
                            "WHERE ID_Biaya = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@NoBukti", noBukti);
                            cmd.Parameters.AddWithValue("@ID", idBiaya);
                            cmd.ExecuteNonQuery();
                        }
                        // Jurnal: Debit Biaya xxx [jumlah]; Kredit Kas.
                        JurnalHelper.TulisJurnalBiaya(conn, trans, tanggal,
                            kode, jumlah, idBiaya);
                        trans.Commit();
                        MessageBox.Show("Biaya " + Rp(jumlah) + " dicatat (" + noBukti + ").");
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        MessageBox.Show("Gagal mencatat biaya: " + ex.Message);
                        return;
                    }
                }
            }
            numJumlahBiaya.Value = 0;
            txtKetBiaya.Clear();
            LoadBiaya();
        }

        // ==================== NERACA ====================
        private void btnTampilNeraca_Click(object sender, EventArgs e)
        {
            TampilNeraca();
        }

        private string NamaAkun(SQLiteConnection conn, string kode)
        {
            using (var cmd = new SQLiteCommand(
                "SELECT Kode || ' - ' || Nama FROM Akun WHERE Kode = @Kode", conn))
            {
                cmd.Parameters.AddWithValue("@Kode", kode);
                object hasil = cmd.ExecuteScalar();
                return hasil == null ? kode : hasil.ToString();
            }
        }

        private void TampilNeraca()
        {
            DateTime periode = dtpPeriodeNeraca.Value;
            DateTime akhirBulan = new DateTime(periode.Year, periode.Month,
                DateTime.DaysInMonth(periode.Year, periode.Month));
            string akhir = akhirBulan.ToString("yyyy-MM-dd 23:59:59");
            string awalTahun = new DateTime(periode.Year, 1, 1)
                .ToString("yyyy-MM-dd 00:00:00");
            neracaAktivaTable = new DataTable();
            neracaAktivaTable.Columns.Add("Akun", typeof(string));
            neracaAktivaTable.Columns.Add("Saldo", typeof(decimal));
            neracaPasivaTable = new DataTable();
            neracaPasivaTable.Columns.Add("Akun", typeof(string));
            neracaPasivaTable.Columns.Add("Saldo", typeof(decimal));
            totalAktivaCetak = 0;
            totalPasivaCetak = 0;
            using (SQLiteConnection conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                foreach (string kode in AkunAktiva)
                {
                    decimal saldo = JurnalHelper.SaldoAkunSampai(conn, null, kode, akhir);
                    neracaAktivaTable.Rows.Add(NamaAkun(conn, kode), saldo);
                    totalAktivaCetak += saldo;
                }
                foreach (string kode in AkunPasiva)
                {
                    decimal saldo = JurnalHelper.SaldoAkunSampai(conn, null, kode, akhir);
                    neracaPasivaTable.Rows.Add(NamaAkun(conn, kode), saldo);
                    totalPasivaCetak += saldo;
                }
                // Laba Berjalan = Pendapatan - HPP - Biaya, sejak 1 Januari
                // sampai akhir periode. GerakanAkunPeriode sudah bertanda
                // sesuai SaldoNormal (Pendapatan = Kredit - Debit, HPP/Biaya
                // = Debit - Kredit), jadi rumusnya tinggal jumlah/kurang.
                decimal pendapatan = JurnalHelper.GerakanAkunPeriode(conn, null,
                        "4101", awalTahun, akhir) +
                    JurnalHelper.GerakanAkunPeriode(conn, null,
                        "4102", awalTahun, akhir);
                decimal hpp = JurnalHelper.GerakanAkunPeriode(conn, null,
                    "5101", awalTahun, akhir);
                decimal biaya = 0;
                foreach (string kode in AkunBiaya)
                    biaya += JurnalHelper.GerakanAkunPeriode(conn, null,
                        kode, awalTahun, akhir);
                decimal labaBerjalan = pendapatan - hpp - biaya;
                neracaPasivaTable.Rows.Add("Laba Berjalan", labaBerjalan);
                totalPasivaCetak += labaBerjalan;
            }
            dgvAktiva.DataSource = neracaAktivaTable;
            if (dgvAktiva.Columns.Contains("Akun")) dgvAktiva.Columns["Akun"].Width = 260;
            if (dgvAktiva.Columns.Contains("Saldo")) dgvAktiva.Columns["Saldo"].DefaultCellStyle.Format = "N0";
            dgvPasiva.DataSource = neracaPasivaTable;
            if (dgvPasiva.Columns.Contains("Akun")) dgvPasiva.Columns["Akun"].Width = 260;
            if (dgvPasiva.Columns.Contains("Saldo")) dgvPasiva.Columns["Saldo"].DefaultCellStyle.Format = "N0";
            lblTotalAktiva.Text = "Total Aktiva: " + Rp(totalAktivaCetak);
            lblTotalPasiva.Text = "Total Pasiva: " + Rp(totalPasivaCetak);
            decimal selisih = totalAktivaCetak - totalPasivaCetak;
            lblSelisihNeraca.Text = "Selisih: " + Rp(selisih) +
                (selisih == 0 ? "  (Seimbang)" : "  (TIDAK SEIMBANG)");
            lblSelisihNeraca.ForeColor = selisih == 0
                ? Theme.SuccessDark : Theme.DangerDark;
        }

        private void btnCetakNeraca_Click(object sender, EventArgs e)
        {
            List<string> baris = new List<string>();
            baris.Add("**NERACA");
            baris.Add("Periode: " + dtpPeriodeNeraca.Value.ToString("MMMM yyyy"));
            baris.Add("");
            baris.Add("**AKTIVA");
            foreach (DataRow row in neracaAktivaTable.Rows)
                baris.Add(row["Akun"] + "  |  " +
                    Convert.ToDecimal(row["Saldo"]).ToString("N0"));
            baris.Add("**Total Aktiva: " + Rp(totalAktivaCetak));
            baris.Add("");
            baris.Add("**PASIVA");
            foreach (DataRow row in neracaPasivaTable.Rows)
                baris.Add(row["Akun"] + "  |  " +
                    Convert.ToDecimal(row["Saldo"]).ToString("N0"));
            baris.Add("**Total Pasiva: " + Rp(totalPasivaCetak));
            baris.Add("**" + lblSelisihNeraca.Text);
            CetakTeks("Neraca", baris);
        }

        // ==================== LABA RUGI ====================
        private void btnTampilLR_Click(object sender, EventArgs e)
        {
            TampilLabaRugi();
        }

        private void TampilLabaRugi()
        {
            DateTime periode = dtpPeriodeLR.Value;
            DateTime awalPeriode;
            DateTime akhirPeriode = new DateTime(periode.Year, periode.Month,
                DateTime.DaysInMonth(periode.Year, periode.Month));
            if (rdoYTD.Checked)
                awalPeriode = new DateTime(periode.Year, 1, 1);
            else
                awalPeriode = new DateTime(periode.Year, periode.Month, 1);
            string awal = awalPeriode.ToString("yyyy-MM-dd 00:00:00");
            string akhir = akhirPeriode.ToString("yyyy-MM-dd 23:59:59");
            lrTable = new DataTable();
            lrTable.Columns.Add("Uraian", typeof(string));
            lrTable.Columns.Add("Jumlah", typeof(decimal));
            List<int> barisTebal = new List<int>();
            decimal pendPenjualan;
            decimal pendLain;
            decimal hpp;
            Dictionary<string, decimal> biayaPerAkun = new Dictionary<string, decimal>();
            using (SQLiteConnection conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                pendPenjualan = JurnalHelper.GerakanAkunPeriode(conn, null,
                    "4101", awal, akhir);
                pendLain = JurnalHelper.GerakanAkunPeriode(conn, null,
                    "4102", awal, akhir);
                hpp = JurnalHelper.GerakanAkunPeriode(conn, null,
                    "5101", awal, akhir);
                foreach (string kode in AkunBiaya)
                    biayaPerAkun[kode] = JurnalHelper.GerakanAkunPeriode(
                        conn, null, kode, awal, akhir);
            }
            decimal totalPendapatan = pendPenjualan + pendLain;
            decimal labaKotor = totalPendapatan - hpp;
            decimal totalBiaya = 0;
            foreach (decimal nilai in biayaPerAkun.Values) totalBiaya += nilai;
            decimal labaBersih = labaKotor - totalBiaya;
            lrTable.Rows.Add("Pendapatan Penjualan", pendPenjualan);
            lrTable.Rows.Add("Pendapatan Lain-lain", pendLain);
            barisTebal.Add(lrTable.Rows.Count);
            lrTable.Rows.Add("Total Pendapatan", totalPendapatan);
            lrTable.Rows.Add("Harga Pokok Penjualan", hpp);
            barisTebal.Add(lrTable.Rows.Count);
            lrTable.Rows.Add("Laba Kotor", labaKotor);
            foreach (string kode in AkunBiaya)
            {
                string nama;
                using (SQLiteConnection conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    nama = NamaAkun(conn, kode);
                }
                lrTable.Rows.Add(nama, biayaPerAkun[kode]);
            }
            barisTebal.Add(lrTable.Rows.Count);
            lrTable.Rows.Add("Total Biaya", totalBiaya);
            barisTebal.Add(lrTable.Rows.Count);
            lrTable.Rows.Add("Laba Bersih", labaBersih);
            dgvLR.DataSource = lrTable;
            if (dgvLR.Columns.Contains("Uraian")) dgvLR.Columns["Uraian"].Width = 340;
            if (dgvLR.Columns.Contains("Jumlah")) dgvLR.Columns["Jumlah"].DefaultCellStyle.Format = "N0";
            foreach (int idx in barisTebal)
                if (idx < dgvLR.Rows.Count)
                    dgvLR.Rows[idx].DefaultCellStyle.Font = Theme.Font(10, FontStyle.Bold);
            string judulPeriode = rdoYTD.Checked
                ? "YTD " + periode.ToString("MMMM yyyy") + " (Jan - " + periode.ToString("MMMM") + ")"
                : periode.ToString("MMMM yyyy");
            lblLabaBersih.Text = "Laba Bersih " + judulPeriode + ": " + Rp(labaBersih);
            lblLabaBersih.ForeColor = labaBersih >= 0
                ? Theme.SuccessDark : Theme.DangerDark;
        }

        private void btnCetakLR_Click(object sender, EventArgs e)
        {
            List<string> baris = new List<string>();
            baris.Add("**LABA RUGI");
            baris.Add("Periode: " + (rdoYTD.Checked
                ? "YTD " + dtpPeriodeLR.Value.ToString("MMMM yyyy")
                : dtpPeriodeLR.Value.ToString("MMMM yyyy")));
            baris.Add("");
            foreach (DataRow row in lrTable.Rows)
                baris.Add(row["Uraian"] + "  |  " +
                    Convert.ToDecimal(row["Jumlah"]).ToString("N0"));
            baris.Add("");
            baris.Add("**" + lblLabaBersih.Text);
            CetakTeks("Laba Rugi", baris);
        }

        // ==================== SALDO AWAL ====================
        private void LoadSaldoAwal()
        {
            using (SQLiteConnection conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT ID_Akun, Kode, Nama, Jenis, SaldoNormal, SaldoAwal " +
                    "FROM Akun WHERE Jenis IN ('Aktiva','Kewajiban','Modal') " +
                    "AND IsAktif = 1 ORDER BY Kode", conn))
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                {
                    saldoAwalTable = new DataTable();
                    adapter.Fill(saldoAwalTable);
                }
            }
            dgvSaldoAwal.DataSource = saldoAwalTable;
            if (dgvSaldoAwal.Columns.Contains("ID_Akun")) dgvSaldoAwal.Columns["ID_Akun"].Visible = false;
            if (dgvSaldoAwal.Columns.Contains("Kode")) dgvSaldoAwal.Columns["Kode"].Width = 80;
            if (dgvSaldoAwal.Columns.Contains("Nama")) dgvSaldoAwal.Columns["Nama"].Width = 220;
            if (dgvSaldoAwal.Columns.Contains("Jenis")) dgvSaldoAwal.Columns["Jenis"].Width = 100;
            if (dgvSaldoAwal.Columns.Contains("SaldoNormal")) dgvSaldoAwal.Columns["SaldoNormal"].Width = 110;
            if (dgvSaldoAwal.Columns.Contains("SaldoAwal")) dgvSaldoAwal.Columns["SaldoAwal"].DefaultCellStyle.Format = "N0";
        }

        private void dgvSaldoAwal_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = dgvSaldoAwal.Rows[e.RowIndex];
            idAkunSA = Convert.ToInt32(row.Cells["ID_Akun"].Value);
            lblAkunSA.Text = "Akun: " + row.Cells["Kode"].Value + " - " +
                row.Cells["Nama"].Value + " (" + row.Cells["SaldoNormal"].Value + "-normal)";
            numSaldoAwal.Value = Convert.ToDecimal(row.Cells["SaldoAwal"].Value);
        }

        private void btnSimpanSA_Click(object sender, EventArgs e)
        {
            if (idAkunSA == null)
            {
                MessageBox.Show("Pilih akun di grid dulu.");
                return;
            }
            using (SQLiteConnection conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "UPDATE Akun SET SaldoAwal = @Nilai WHERE ID_Akun = @ID", conn))
                {
                    cmd.Parameters.AddWithValue("@Nilai", numSaldoAwal.Value);
                    cmd.Parameters.AddWithValue("@ID", idAkunSA.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            MessageBox.Show("Saldo awal disimpan.");
            LoadSaldoAwal();
        }
    }
}
