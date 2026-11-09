using System;
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
    /// Modul Persediaan &amp; Gudang (Fase 3), 5 tab:
    ///
    /// KARTU STOK: riwayat keluar-masuk per item + lokasi + rentang
    /// tanggal, dibaca dari StokMovement. Saldo Awal = stok lokasi saat
    /// ini - jumlah pergerakan dalam rentang (tetap benar walau ada stok
    /// awal tanpa log, mis. data contoh); Sisa berjalan dihitung dari
    /// Qty per baris. Penjualan titipan konsinyasi tidak tampil di sini
    /// (barangnya tidak pernah masuk stok lokasi).
    ///
    /// PERAKITAN: resep (Rakitan + RakitanDetail) + "Proses Rakit". Bahan
    /// dicek dulu di lokasi 1; kalau kurang, proses ditolak dengan pesan
    /// yang menyebut itemnya. Dalam SATU transaksi: bahan berkurang
    /// (batch dikonsumsi HPP, log 'Rakit Keluar'), barang jadi bertambah
    /// + batch baru seharga total HPP bahan / qty (log 'Rakit Masuk').
    ///
    /// TRANSFER: daftar lokasi (tambah/ubah via modal) + transfer antar
    /// lokasi. Draft bisa diubah / dibatalkan. "Selesaikan" dalam SATU
    /// transaksi: stok asal berkurang, tujuan bertambah, total Item.Stok
    /// TIDAK berubah, batch dipindah tertua dulu (HargaBeli dan
    /// TanggalMasuk dipertahankan), log 'Transfer Keluar' / 'Transfer
    /// Masuk' ditulis. Transfer Selesai terkunci.
    ///
    /// OPNAME: buat opname per lokasi (semua item Fisik/Rakitan dengan
    /// StokSistem dari StokLokasi), isi StokFisik, Selisih otomatis.
    /// "Selesaikan" dalam SATU transaksi: StokLokasi = StokFisik, total
    /// Item.Stok disesuaikan, kekurangan mengonsumsi batch, kelebihan
    /// membuat batch baru seharga HargaBeli item, log 'Opname' ditulis.
    /// Opname Selesai terkunci dan hanya bisa dilihat.
    ///
    /// HPP: pilih metode FIFO / LIFO (disimpan di Pengaturan) + daftar
    /// batch aktif (QtySisa &gt; 0) untuk melihat dasar perhitungan HPP.
    ///
    /// UI dibangun dari kode (tanpa Designer) seperti PembelianControl.
    /// </summary>
    public partial class PersediaanControl : UserControl
    {
        private Guna2TabControl tabControl;
        private TabPage tabKartu;
        private TabPage tabRakit;
        private TabPage tabTransfer;
        private TabPage tabOpname;
        private TabPage tabHPP;

        // ----- Kartu Stok -----
        private Guna2ComboBox cmbItemKartu;
        private Guna2ComboBox cmbLokasiKartu;
        private Guna2DateTimePicker dtpDariKartu;
        private Guna2DateTimePicker dtpSampaiKartu;
        private Guna2Button btnTampilkanKartu;
        private Guna2DataGridView dgvKartu;
        private Label lblSaldoAwalKartu;
        private Label lblStokAkhirKartu;
        private Guna2Button btnCetakKartu;
        private DataTable kartuTable;
        private DataTable itemKartuTable;
        private DataTable lokasiKartuTable;
        // ----- Cetak kartu stok -----
        private PrintDocument docKartu;
        private DataTable printKartu;
        private string printItemKartu = "";
        private string printLokasiKartu = "";
        private string printPeriodeKartu = "";
        private decimal printSaldoAwalKartu;
        private decimal printStokAkhirKartu;
        private int printIndexKartu;

        // ----- Perakitan -----
        private Guna2ComboBox cmbResep;
        private Guna2ComboBox cmbItemJadi;
        private Guna2Button btnResepBaru;
        private Guna2Button btnSimpanResep;
        private Guna2Button btnHapusResep;
        private Guna2ComboBox cmbBahanRakit;
        private Guna2NumericUpDown numQtyBahan;
        private Guna2Button btnTambahBahan;
        private Guna2Button btnHapusBahan;
        private Guna2DataGridView dgvResep;
        private Guna2ComboBox cmbProsesRakit;
        private Guna2NumericUpDown numQtyRakit;
        private Guna2Button btnProsesRakit;
        private Label lblInfoRakit;
        private DataTable resepTable;
        private DataTable resepDetailTable;
        private DataTable itemJadiTable;
        private DataTable bahanTable;
        private DataTable prosesRakitTable;
        // Resep yang sedang dibuka (null = resep baru).
        private int? idRakitanPilihan;

        // ----- Transfer -----
        private Guna2DataGridView dgvLokasi;
        private Guna2Button btnTambahLokasi;
        private Guna2Button btnUbahLokasi;
        private Guna2ComboBox cmbAsal;
        private Guna2ComboBox cmbTujuan;
        private Guna2ComboBox cmbItemTransfer;
        private Guna2NumericUpDown numQtyTransfer;
        private Guna2Button btnTambahItemTransfer;
        private Guna2DataGridView dgvTransferDetail;
        private Guna2DataGridView dgvTransfer;
        private Guna2TextBox txtCatatanTransfer;
        private Guna2Button btnSimpanTransfer;
        private Guna2Button btnSelesaikanTransfer;
        private Guna2Button btnBatalkanTransfer;
        private Guna2Button btnBaruTransfer;
        private DataTable lokasiTable;
        private DataTable lokasiAsalTable;
        private DataTable lokasiTujuanTable;
        private DataTable itemTransferTable;
        private DataTable transferDetailTable;
        private DataTable transferTable;
        // Transfer yang sedang dibuka (null = transfer baru).
        private int? idTransferPilihan;
        private string statusTransferPilihan;

        // ----- Opname -----
        private Guna2ComboBox cmbLokasiOpname;
        private Guna2Button btnBuatOpname;
        private Guna2Button btnSimpanOpname;
        private Guna2Button btnSelesaikanOpname;
        private Guna2Button btnHapusOpname;
        private Guna2DataGridView dgvOpname;
        private Guna2DataGridView dgvOpnameDetail;
        private DataTable opnameTable;
        private DataTable opnameDetailTable;
        private DataTable lokasiOpnameTable;
        // Opname yang sedang dibuka (null = belum ada yang dipilih).
        private int? idOpnamePilihan;
        private string statusOpnamePilihan;
        // True saat detail opname sedang dimuat — event edit diabaikan.
        private bool memuatOpname;

        // ----- HPP -----
        private Guna2ComboBox cmbHPPMethod;
        private Label lblPenjelasanHPP;
        private Guna2Button btnSimpanHPP;
        private Guna2DataGridView dgvBatch;
        private DataTable batchTable;

        public PersediaanControl()
        {
            InitializeComponent();
            TerapkanTema();
            LoadComboKartu();
            LoadResep();
            LoadLokasiSemua();
            LoadTransfer();
            LoadOpname();
            LoadHPP();
            dtpDariKartu.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dtpSampaiKartu.Value = DateTime.Now.Date;
        }

        /// <summary>Dipanggil FormMain setiap kali tab Persediaan dibuka.</summary>
        public void RefreshData()
        {
            LoadComboKartu();
            LoadResep();
            LoadLokasiSemua();
            LoadTransfer();
            LoadOpname();
            LoadHPP();
        }

        // ==================== PEMBANGUN UI ====================
        private void InitializeComponent()
        {
            this.tabControl = new Guna.UI2.WinForms.Guna2TabControl();
            this.tabKartu = new System.Windows.Forms.TabPage();
            this.tabTransfer = new System.Windows.Forms.TabPage();
            this.tabOpname = new System.Windows.Forms.TabPage();
            this.tabRakit = new System.Windows.Forms.TabPage();
            this.tabHPP = new System.Windows.Forms.TabPage();
            this.tabControl.Controls.Add(this.tabKartu);
            this.tabControl.Controls.Add(this.tabRakit);
            this.tabControl.Controls.Add(this.tabTransfer);
            this.tabControl.Controls.Add(this.tabOpname);
            this.tabControl.Controls.Add(this.tabHPP);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Name = "tabControl";
            this.tabKartu.Text = "Kartu Stok";
            this.tabRakit.Text = "Perakitan";
            this.tabTransfer.Text = "Transfer";
            this.tabOpname.Text = "Stok Opname";
            this.tabHPP.Text = "HPP";

            BangunTabKartu();
            BangunTabRakit();
            BangunTabTransfer();
            BangunTabOpname();
            BangunTabHPP();

            this.Controls.Add(this.tabControl);
            this.Name = "PersediaanControl";
            this.Size = new System.Drawing.Size(900, 620);
            var header = Theme.BuildHeader("Persediaan", "Kartu stok, perakitan, transfer, opname, dan HPP");
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

        private Guna2DataGridView BuatGrid(string nama, int x, int y, int tinggi)
        {
            Guna2DataGridView grid = new Guna2DataGridView();
            grid.Name = nama;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grid.Location = new Point(x, y);
            grid.ReadOnly = true;
            grid.Size = new Size(828, tinggi);
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            return grid;
        }

        private void BangunTabKartu()
        {
            cmbItemKartu = new Guna2ComboBox();
            cmbItemKartu.Name = "cmbItemKartu";
            cmbItemKartu.Font = Theme.Font(10);
            cmbItemKartu.Location = new Point(12, 32);
            cmbItemKartu.Size = new Size(280, 30);
            cmbItemKartu.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLokasiKartu = new Guna2ComboBox();
            cmbLokasiKartu.Name = "cmbLokasiKartu";
            cmbLokasiKartu.Font = Theme.Font(10);
            cmbLokasiKartu.Location = new Point(304, 32);
            cmbLokasiKartu.Size = new Size(180, 30);
            cmbLokasiKartu.DropDownStyle = ComboBoxStyle.DropDownList;
            dtpDariKartu = new Guna2DateTimePicker();
            dtpDariKartu.Name = "dtpDariKartu";
            dtpDariKartu.Font = Theme.Font(10);
            dtpDariKartu.Location = new Point(496, 32);
            dtpDariKartu.Size = new Size(120, 30);
            dtpSampaiKartu = new Guna2DateTimePicker();
            dtpSampaiKartu.Name = "dtpSampaiKartu";
            dtpSampaiKartu.Font = Theme.Font(10);
            dtpSampaiKartu.Location = new Point(628, 32);
            dtpSampaiKartu.Size = new Size(120, 30);
            btnTampilkanKartu = BuatButton("btnTampilkanKartu", "Tampilkan", 760, 28, btnTampilkanKartu_Click);
            btnTampilkanKartu.Size = new Size(90, 36);

            kartuTable = new DataTable();
            kartuTable.Columns.Add("Tanggal", typeof(string));
            kartuTable.Columns.Add("Jenis", typeof(string));
            kartuTable.Columns.Add("Keterangan", typeof(string));
            kartuTable.Columns.Add("Masuk", typeof(decimal));
            kartuTable.Columns.Add("Keluar", typeof(decimal));
            kartuTable.Columns.Add("Sisa", typeof(decimal));
            dgvKartu = BuatGrid("dgvKartu", 12, 76, 360);
            dgvKartu.DataSource = kartuTable;
            if (dgvKartu.Columns["Masuk"] != null)
                dgvKartu.Columns["Masuk"].DefaultCellStyle.Format = "N0";
            if (dgvKartu.Columns["Keluar"] != null)
                dgvKartu.Columns["Keluar"].DefaultCellStyle.Format = "N0";
            if (dgvKartu.Columns["Sisa"] != null)
                dgvKartu.Columns["Sisa"].DefaultCellStyle.Format = "N0";

            lblSaldoAwalKartu = BuatLabel("Saldo Awal: -", 12, 452);
            lblSaldoAwalKartu.Font = Theme.Font(11, FontStyle.Bold);
            lblStokAkhirKartu = BuatLabel("Stok Akhir: -", 12, 478);
            lblStokAkhirKartu.Font = Theme.Font(11, FontStyle.Bold);
            btnCetakKartu = BuatButton("btnCetakKartu", "Cetak", 700, 452, btnCetakKartu_Click);
            btnCetakKartu.Size = new Size(140, 42);

            tabKartu.Controls.Add(BuatLabel("Barang", 12, 12));
            tabKartu.Controls.Add(cmbItemKartu);
            tabKartu.Controls.Add(BuatLabel("Lokasi", 304, 12));
            tabKartu.Controls.Add(cmbLokasiKartu);
            tabKartu.Controls.Add(BuatLabel("Dari", 496, 12));
            tabKartu.Controls.Add(dtpDariKartu);
            tabKartu.Controls.Add(BuatLabel("Sampai", 628, 12));
            tabKartu.Controls.Add(dtpSampaiKartu);
            tabKartu.Controls.Add(btnTampilkanKartu);
            tabKartu.Controls.Add(dgvKartu);
            tabKartu.Controls.Add(lblSaldoAwalKartu);
            tabKartu.Controls.Add(lblStokAkhirKartu);
            tabKartu.Controls.Add(btnCetakKartu);
        }

        private void BangunTabRakit()
        {
            cmbResep = new Guna2ComboBox();
            cmbResep.Name = "cmbResep";
            cmbResep.Font = Theme.Font(10);
            cmbResep.Location = new Point(12, 32);
            cmbResep.Size = new Size(240, 30);
            cmbResep.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbResep.SelectedIndexChanged += cmbResep_SelectedIndexChanged;
            cmbItemJadi = new Guna2ComboBox();
            cmbItemJadi.Name = "cmbItemJadi";
            cmbItemJadi.Font = Theme.Font(10);
            cmbItemJadi.Location = new Point(264, 32);
            cmbItemJadi.Size = new Size(200, 30);
            cmbItemJadi.DropDownStyle = ComboBoxStyle.DropDownList;
            btnResepBaru = BuatButton("btnResepBaru", "Resep Baru", 476, 28, btnResepBaru_Click);
            btnResepBaru.Size = new Size(110, 36);
            btnSimpanResep = BuatButton("btnSimpanResep", "Simpan Resep", 598, 28, btnSimpanResep_Click);
            btnSimpanResep.Size = new Size(120, 36);
            btnHapusResep = BuatButton("btnHapusResep", "Hapus Resep", 730, 28, btnHapusResep_Click);
            btnHapusResep.Size = new Size(120, 36);

            cmbBahanRakit = new Guna2ComboBox();
            cmbBahanRakit.Name = "cmbBahanRakit";
            cmbBahanRakit.Font = Theme.Font(10);
            cmbBahanRakit.Location = new Point(12, 96);
            cmbBahanRakit.Size = new Size(280, 30);
            cmbBahanRakit.DropDownStyle = ComboBoxStyle.DropDownList;
            numQtyBahan = new Guna2NumericUpDown();
            numQtyBahan.Name = "numQtyBahan";
            numQtyBahan.Font = Theme.Font(10);
            numQtyBahan.Location = new Point(304, 96);
            numQtyBahan.Size = new Size(80, 30);
            numQtyBahan.Minimum = 1;
            numQtyBahan.Maximum = 99999;
            numQtyBahan.Value = 1;
            btnTambahBahan = BuatButton("btnTambahBahan", "Tambah Bahan", 396, 92, btnTambahBahan_Click);
            btnTambahBahan.Size = new Size(130, 36);
            btnHapusBahan = BuatButton("btnHapusBahan", "Hapus Bahan", 538, 92, btnHapusBahan_Click);
            btnHapusBahan.Size = new Size(120, 36);

            resepDetailTable = new DataTable();
            resepDetailTable.Columns.Add("ID_RakitanDetail", typeof(int));
            resepDetailTable.Columns.Add("ID_ItemBahan", typeof(int));
            resepDetailTable.Columns.Add("Nama", typeof(string));
            resepDetailTable.Columns.Add("Qty", typeof(decimal));
            dgvResep = BuatGrid("dgvResep", 12, 140, 110);
            dgvResep.DataSource = resepDetailTable;
            dgvResep.CellDoubleClick += dgvResep_CellDoubleClick;
            if (dgvResep.Columns["ID_RakitanDetail"] != null)
                dgvResep.Columns["ID_RakitanDetail"].Visible = false;
            if (dgvResep.Columns["ID_ItemBahan"] != null)
                dgvResep.Columns["ID_ItemBahan"].Visible = false;
            if (dgvResep.Columns["Qty"] != null)
                dgvResep.Columns["Qty"].DefaultCellStyle.Format = "N0";

            cmbProsesRakit = new Guna2ComboBox();
            cmbProsesRakit.Name = "cmbProsesRakit";
            cmbProsesRakit.Font = Theme.Font(10);
            cmbProsesRakit.Location = new Point(12, 290);
            cmbProsesRakit.Size = new Size(280, 30);
            cmbProsesRakit.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbProsesRakit.SelectedIndexChanged += cmbProsesRakit_SelectedIndexChanged;
            numQtyRakit = new Guna2NumericUpDown();
            numQtyRakit.Name = "numQtyRakit";
            numQtyRakit.Font = Theme.Font(10);
            numQtyRakit.Location = new Point(304, 290);
            numQtyRakit.Size = new Size(80, 30);
            numQtyRakit.Minimum = 1;
            numQtyRakit.Maximum = 99999;
            numQtyRakit.Value = 1;
            numQtyRakit.ValueChanged += numQtyRakit_ValueChanged;
            btnProsesRakit = BuatButton("btnProsesRakit", "Proses Rakit", 396, 286, btnProsesRakit_Click);
            btnProsesRakit.Size = new Size(140, 36);
            lblInfoRakit = BuatLabel("", 12, 330);
            lblInfoRakit.Size = new Size(820, 40);

            tabRakit.Controls.Add(BuatLabel("Resep (pilih untuk ubah)", 12, 12));
            tabRakit.Controls.Add(cmbResep);
            tabRakit.Controls.Add(BuatLabel("Barang Jadi (tipe Rakitan)", 264, 12));
            tabRakit.Controls.Add(cmbItemJadi);
            tabRakit.Controls.Add(btnResepBaru);
            tabRakit.Controls.Add(btnSimpanResep);
            tabRakit.Controls.Add(btnHapusResep);
            tabRakit.Controls.Add(BuatLabel("Bahan Baku", 12, 76));
            tabRakit.Controls.Add(cmbBahanRakit);
            tabRakit.Controls.Add(BuatLabel("Qty / unit", 304, 76));
            tabRakit.Controls.Add(numQtyBahan);
            tabRakit.Controls.Add(btnTambahBahan);
            tabRakit.Controls.Add(btnHapusBahan);
            tabRakit.Controls.Add(dgvResep);
            tabRakit.Controls.Add(BuatLabel("Proses Rakit", 12, 270));
            tabRakit.Controls.Add(cmbProsesRakit);
            tabRakit.Controls.Add(BuatLabel("Qty", 304, 270));
            tabRakit.Controls.Add(numQtyRakit);
            tabRakit.Controls.Add(btnProsesRakit);
            tabRakit.Controls.Add(lblInfoRakit);
        }

        private void BangunTabTransfer()
        {
            dgvLokasi = BuatGrid("dgvLokasi", 12, 32, 80);
            btnTambahLokasi = BuatButton("btnTambahLokasi", "Tambah Lokasi", 12, 120, btnTambahLokasi_Click);
            btnTambahLokasi.Size = new Size(150, 36);
            btnUbahLokasi = BuatButton("btnUbahLokasi", "Ubah Lokasi", 174, 120, btnUbahLokasi_Click);
            btnUbahLokasi.Size = new Size(140, 36);

            cmbAsal = new Guna2ComboBox();
            cmbAsal.Name = "cmbAsal";
            cmbAsal.Font = Theme.Font(10);
            cmbAsal.Location = new Point(12, 190);
            cmbAsal.Size = new Size(200, 30);
            cmbAsal.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTujuan = new Guna2ComboBox();
            cmbTujuan.Name = "cmbTujuan";
            cmbTujuan.Font = Theme.Font(10);
            cmbTujuan.Location = new Point(224, 190);
            cmbTujuan.Size = new Size(200, 30);
            cmbTujuan.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbItemTransfer = new Guna2ComboBox();
            cmbItemTransfer.Name = "cmbItemTransfer";
            cmbItemTransfer.Font = Theme.Font(10);
            cmbItemTransfer.Location = new Point(436, 190);
            cmbItemTransfer.Size = new Size(200, 30);
            cmbItemTransfer.DropDownStyle = ComboBoxStyle.DropDownList;
            numQtyTransfer = new Guna2NumericUpDown();
            numQtyTransfer.Name = "numQtyTransfer";
            numQtyTransfer.Font = Theme.Font(10);
            numQtyTransfer.Location = new Point(648, 190);
            numQtyTransfer.Size = new Size(70, 30);
            numQtyTransfer.Minimum = 1;
            numQtyTransfer.Maximum = 99999;
            numQtyTransfer.Value = 1;
            btnTambahItemTransfer = BuatButton("btnTambahItemTransfer", "Tambah Item", 730, 186, btnTambahItemTransfer_Click);
            btnTambahItemTransfer.Size = new Size(110, 36);

            transferDetailTable = new DataTable();
            transferDetailTable.Columns.Add("ID_Item", typeof(int));
            transferDetailTable.Columns.Add("Nama", typeof(string));
            transferDetailTable.Columns.Add("Qty", typeof(decimal));
            dgvTransferDetail = BuatGrid("dgvTransferDetail", 12, 230, 80);
            dgvTransferDetail.DataSource = transferDetailTable;
            dgvTransferDetail.CellDoubleClick += dgvTransferDetail_CellDoubleClick;
            if (dgvTransferDetail.Columns["ID_Item"] != null)
                dgvTransferDetail.Columns["ID_Item"].Visible = false;
            if (dgvTransferDetail.Columns["Qty"] != null)
                dgvTransferDetail.Columns["Qty"].DefaultCellStyle.Format = "N0";

            dgvTransfer = BuatGrid("dgvTransfer", 12, 320, 110);
            dgvTransfer.CellClick += dgvTransfer_CellClick;

            txtCatatanTransfer = new Guna2TextBox();
            txtCatatanTransfer.Name = "txtCatatanTransfer";
            txtCatatanTransfer.Font = Theme.Font(10);
            txtCatatanTransfer.Location = new Point(560, 446);
            txtCatatanTransfer.Size = new Size(280, 30);
            txtCatatanTransfer.PlaceholderText = "Catatan (opsional)";
            btnSimpanTransfer = BuatButton("btnSimpanTransfer", "Simpan", 12, 440, btnSimpanTransfer_Click);
            btnSelesaikanTransfer = BuatButton("btnSelesaikanTransfer", "Selesaikan", 144, 440, btnSelesaikanTransfer_Click);
            btnSelesaikanTransfer.Size = new Size(140, 42);
            btnBatalkanTransfer = BuatButton("btnBatalkanTransfer", "Batalkan", 296, 440, btnBatalkanTransfer_Click);
            btnBatalkanTransfer.Size = new Size(120, 42);
            btnBaruTransfer = BuatButton("btnBaruTransfer", "Baru", 428, 440, btnBaruTransfer_Click);

            tabTransfer.Controls.Add(BuatLabel("Daftar Lokasi / Gudang", 12, 12));
            tabTransfer.Controls.Add(dgvLokasi);
            tabTransfer.Controls.Add(btnTambahLokasi);
            tabTransfer.Controls.Add(btnUbahLokasi);
            tabTransfer.Controls.Add(BuatLabel("Dari", 12, 170));
            tabTransfer.Controls.Add(cmbAsal);
            tabTransfer.Controls.Add(BuatLabel("Ke", 224, 170));
            tabTransfer.Controls.Add(cmbTujuan);
            tabTransfer.Controls.Add(BuatLabel("Item", 436, 170));
            tabTransfer.Controls.Add(cmbItemTransfer);
            tabTransfer.Controls.Add(BuatLabel("Qty", 648, 170));
            tabTransfer.Controls.Add(numQtyTransfer);
            tabTransfer.Controls.Add(btnTambahItemTransfer);
            tabTransfer.Controls.Add(dgvTransferDetail);
            tabTransfer.Controls.Add(dgvTransfer);
            tabTransfer.Controls.Add(BuatLabel("Catatan", 560, 426));
            tabTransfer.Controls.Add(txtCatatanTransfer);
            tabTransfer.Controls.Add(btnSimpanTransfer);
            tabTransfer.Controls.Add(btnSelesaikanTransfer);
            tabTransfer.Controls.Add(btnBatalkanTransfer);
            tabTransfer.Controls.Add(btnBaruTransfer);
            PerbaruiTombolTransfer();
        }

        private void BangunTabOpname()
        {
            cmbLokasiOpname = new Guna2ComboBox();
            cmbLokasiOpname.Name = "cmbLokasiOpname";
            cmbLokasiOpname.Font = Theme.Font(10);
            cmbLokasiOpname.Location = new Point(12, 32);
            cmbLokasiOpname.Size = new Size(200, 30);
            cmbLokasiOpname.DropDownStyle = ComboBoxStyle.DropDownList;
            btnBuatOpname = BuatButton("btnBuatOpname", "Buat Opname", 224, 28, btnBuatOpname_Click);
            btnBuatOpname.Size = new Size(140, 36);
            btnSimpanOpname = BuatButton("btnSimpanOpname", "Simpan Perubahan", 376, 28, btnSimpanOpname_Click);
            btnSimpanOpname.Size = new Size(170, 36);
            btnSelesaikanOpname = BuatButton("btnSelesaikanOpname", "Selesaikan", 558, 28, btnSelesaikanOpname_Click);
            btnSelesaikanOpname.Size = new Size(130, 36);
            btnHapusOpname = BuatButton("btnHapusOpname", "Hapus Draft", 700, 28, btnHapusOpname_Click);
            btnHapusOpname.Size = new Size(130, 36);

            dgvOpname = BuatGrid("dgvOpname", 12, 76, 110);
            dgvOpname.CellClick += dgvOpname_CellClick;
            opnameDetailTable = new DataTable();
            opnameDetailTable.Columns.Add("ID_OpnameDetail", typeof(int));
            opnameDetailTable.Columns.Add("ID_Item", typeof(int));
            opnameDetailTable.Columns.Add("Kode", typeof(string));
            opnameDetailTable.Columns.Add("Nama", typeof(string));
            opnameDetailTable.Columns.Add("StokSistem", typeof(decimal));
            opnameDetailTable.Columns.Add("StokFisik", typeof(decimal));
            opnameDetailTable.Columns.Add("Selisih", typeof(decimal));
            dgvOpnameDetail = BuatGrid("dgvOpnameDetail", 12, 196, 280);
            dgvOpnameDetail.DataSource = opnameDetailTable;
            dgvOpnameDetail.ReadOnly = false;
            dgvOpnameDetail.CellValueChanged += dgvOpnameDetail_CellValueChanged;
            if (dgvOpnameDetail.Columns["ID_OpnameDetail"] != null)
                dgvOpnameDetail.Columns["ID_OpnameDetail"].Visible = false;
            if (dgvOpnameDetail.Columns["ID_Item"] != null)
                dgvOpnameDetail.Columns["ID_Item"].Visible = false;
            if (dgvOpnameDetail.Columns["StokSistem"] != null)
                dgvOpnameDetail.Columns["StokSistem"].DefaultCellStyle.Format = "N0";
            if (dgvOpnameDetail.Columns["StokFisik"] != null)
                dgvOpnameDetail.Columns["StokFisik"].DefaultCellStyle.Format = "N0";
            if (dgvOpnameDetail.Columns["Selisih"] != null)
                dgvOpnameDetail.Columns["Selisih"].DefaultCellStyle.Format = "N0";

            tabOpname.Controls.Add(BuatLabel("Lokasi", 12, 12));
            tabOpname.Controls.Add(cmbLokasiOpname);
            tabOpname.Controls.Add(btnBuatOpname);
            tabOpname.Controls.Add(btnSimpanOpname);
            tabOpname.Controls.Add(btnSelesaikanOpname);
            tabOpname.Controls.Add(btnHapusOpname);
            tabOpname.Controls.Add(dgvOpname);
            tabOpname.Controls.Add(dgvOpnameDetail);
            PerbaruiTombolOpname();
        }

        private void BangunTabHPP()
        {
            cmbHPPMethod = new Guna2ComboBox();
            cmbHPPMethod.Name = "cmbHPPMethod";
            cmbHPPMethod.Font = Theme.Font(10);
            cmbHPPMethod.Location = new Point(12, 32);
            cmbHPPMethod.Size = new Size(200, 30);
            cmbHPPMethod.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbHPPMethod.Items.Add("FIFO");
            cmbHPPMethod.Items.Add("LIFO");
            btnSimpanHPP = BuatButton("btnSimpanHPP", "Simpan Metode", 224, 28, btnSimpanHPP_Click);
            btnSimpanHPP.Size = new Size(150, 36);
            lblPenjelasanHPP = BuatLabel("", 12, 76);
            lblPenjelasanHPP.Size = new Size(820, 60);
            lblPenjelasanHPP.Text = "FIFO (First In, First Out): barang yang dibeli paling dulu dijual " +
                "paling dulu — HPP diambil dari batch tertua. Cocok kalau harga beli " +
                "cenderung naik. LIFO (Last In, First Out): barang yang dibeli paling " +
                "akhir dijual paling dulu — HPP diambil dari batch terbaru. Metode ini " +
                "hanya memengaruhi perhitungan HPP (laba kotor), bukan urutan fisik " +
                "barang di gudang.";
            dgvBatch = BuatGrid("dgvBatch", 12, 150, 300);

            tabHPP.Controls.Add(BuatLabel("Metode HPP", 12, 12));
            tabHPP.Controls.Add(cmbHPPMethod);
            tabHPP.Controls.Add(btnSimpanHPP);
            tabHPP.Controls.Add(lblPenjelasanHPP);
            tabHPP.Controls.Add(dgvBatch);
        }

        private void TerapkanTema()
        {
            this.BackColor = Theme.Background;
            this.Font = Theme.Font(10);
            Theme.StyleTabControl(tabControl);
            Theme.StyleGrid(dgvKartu);
            Theme.StyleGrid(dgvResep);
            Theme.StyleGrid(dgvLokasi);
            Theme.StyleGrid(dgvTransferDetail);
            Theme.StyleGrid(dgvTransfer);
            Theme.StyleGrid(dgvOpname);
            Theme.StyleGrid(dgvOpnameDetail);
            Theme.StyleGrid(dgvBatch);
            Theme.StylePrimaryButton(btnTampilkanKartu);
            Theme.StyleGhostButton(btnCetakKartu);
            Theme.StylePrimaryButton(btnResepBaru);
            Theme.StyleSuccessButton(btnSimpanResep);
            Theme.StyleDangerButton(btnHapusResep);
            Theme.StylePrimaryButton(btnTambahBahan);
            Theme.StyleGhostButton(btnHapusBahan);
            Theme.StyleSuccessButton(btnProsesRakit);
            Theme.StylePrimaryButton(btnTambahLokasi);
            Theme.StyleGhostButton(btnUbahLokasi);
            Theme.StylePrimaryButton(btnTambahItemTransfer);
            Theme.StylePrimaryButton(btnSimpanTransfer);
            Theme.StyleSuccessButton(btnSelesaikanTransfer);
            Theme.StyleDangerButton(btnBatalkanTransfer);
            Theme.StyleGhostButton(btnBaruTransfer);
            Theme.StylePrimaryButton(btnBuatOpname);
            Theme.StylePrimaryButton(btnSimpanOpname);
            Theme.StyleSuccessButton(btnSelesaikanOpname);
            Theme.StyleDangerButton(btnHapusOpname);
            Theme.StylePrimaryButton(btnSimpanHPP);
            Theme.StyleComboBox(cmbItemKartu);
            Theme.StyleComboBox(cmbLokasiKartu);
            Theme.StyleComboBox(cmbResep);
            Theme.StyleComboBox(cmbItemJadi);
            Theme.StyleComboBox(cmbBahanRakit);
            Theme.StyleComboBox(cmbProsesRakit);
            Theme.StyleComboBox(cmbAsal);
            Theme.StyleComboBox(cmbTujuan);
            Theme.StyleComboBox(cmbItemTransfer);
            Theme.StyleComboBox(cmbLokasiOpname);
            Theme.StyleComboBox(cmbHPPMethod);
            Theme.StyleDatePicker(dtpDariKartu);
            Theme.StyleDatePicker(dtpSampaiKartu);
            Theme.StyleNumericUpDown(numQtyBahan);
            Theme.StyleNumericUpDown(numQtyRakit);
            Theme.StyleNumericUpDown(numQtyTransfer);
            Theme.StyleTextBox(txtCatatanTransfer);
            lblSaldoAwalKartu.ForeColor = Theme.Text;
            lblStokAkhirKartu.ForeColor = Theme.Text;
            lblInfoRakit.ForeColor = Theme.Muted;
            lblPenjelasanHPP.ForeColor = Theme.Muted;
        }

        // ==================== HELPER ====================
        private static string Sekarang()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private static string TanggalSaja(DateTime t)
        {
            return t.ToString("yyyy-MM-dd");
        }

        // ==================== KARTU STOK ====================
        private void LoadComboKartu()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Item, Nama FROM Item WHERE Tipe IN ('Fisik','Rakitan') ORDER BY Nama", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        itemKartuTable = new DataTable();
                        adapter.Fill(itemKartuTable);
                        cmbItemKartu.DisplayMember = "Nama";
                        cmbItemKartu.ValueMember = "ID_Item";
                        cmbItemKartu.DataSource = itemKartuTable;
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Lokasi, Nama FROM Lokasi WHERE IsAktif = 1 ORDER BY ID_Lokasi", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        lokasiKartuTable = new DataTable();
                        adapter.Fill(lokasiKartuTable);
                        cmbLokasiKartu.DisplayMember = "Nama";
                        cmbLokasiKartu.ValueMember = "ID_Lokasi";
                        cmbLokasiKartu.DataSource = lokasiKartuTable;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat combo kartu stok:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnTampilkanKartu_Click(object sender, EventArgs e)
        {
            if (cmbItemKartu.SelectedValue == null || cmbLokasiKartu.SelectedValue == null)
            {
                MessageBox.Show("Pilih barang dan lokasi dulu!");
                return;
            }
            int idItem = Convert.ToInt32(cmbItemKartu.SelectedValue);
            int idLokasi = Convert.ToInt32(cmbLokasiKartu.SelectedValue);
            string dari = TanggalSaja(dtpDariKartu.Value.Date);
            // Sampai inklusif: batas atas = hari setelah Sampai.
            string sampaiBesok = TanggalSaja(dtpSampaiKartu.Value.Date.AddDays(1));
            if (dtpSampaiKartu.Value.Date < dtpDariKartu.Value.Date)
            {
                MessageBox.Show("Tanggal Sampai tidak boleh sebelum Dari!");
                return;
            }
            try
            {
                decimal stokAkhir = 0;
                DataTable gerakan = new DataTable();
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    // Stok lokasi saat ini (fallback ke Item.Stok kalau
                    // belum ada baris StokLokasi — item lama).
                    using (var cmd = new SQLiteCommand(
                        "SELECT COALESCE((SELECT Qty FROM StokLokasi " +
                        "WHERE ID_Lokasi = @Lokasi AND ID_Item = @ID), Stok) " +
                        "FROM Item WHERE ID_Item = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@Lokasi", idLokasi);
                        cmd.Parameters.AddWithValue("@ID", idItem);
                        stokAkhir = Convert.ToDecimal(cmd.ExecuteScalar());
                    }
                    // Pergerakan dalam rentang untuk lokasi ini. Log lama
                    // (ID_Lokasi NULL) dianggap lokasi 1. Penjualan titipan
                    // konsinyasi tidak ikut (barangnya tidak pernah masuk
                    // stok lokasi).
                    using (var cmd = new SQLiteCommand(
                        "SELECT Tanggal, Jenis, Qty, StokSesudah, Keterangan " +
                        "FROM StokMovement WHERE ID_Item = @ID " +
                        "AND Tanggal >= @Dari AND Tanggal < @Sampai " +
                        "AND (ID_Lokasi = @Lokasi OR (ID_Lokasi IS NULL AND @Lokasi = 1)) " +
                        "AND Jenis NOT IN ('Jual Konsinyasi', 'Jual Konsinyasi Keluar') " +
                        "ORDER BY Tanggal, ID_Movement", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idItem);
                        cmd.Parameters.AddWithValue("@Dari", dari);
                        cmd.Parameters.AddWithValue("@Sampai", sampaiBesok);
                        cmd.Parameters.AddWithValue("@Lokasi", idLokasi);
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        adapter.Fill(gerakan);
                    }
                }
                decimal jumlahGerakan = 0;
                foreach (DataRow baris in gerakan.Rows)
                    jumlahGerakan += Convert.ToDecimal(baris["Qty"]);
                // Saldo Awal = stok sekarang - pergerakan dalam rentang.
                // Tetap benar walau ada stok awal tanpa log (data contoh).
                decimal saldoAwal = stokAkhir - jumlahGerakan;
                kartuTable.Clear();
                decimal berjalan = saldoAwal;
                foreach (DataRow baris in gerakan.Rows)
                {
                    decimal qty = Convert.ToDecimal(baris["Qty"]);
                    berjalan += qty;
                    DataRow tampil = kartuTable.NewRow();
                    tampil["Tanggal"] = Convert.ToString(baris["Tanggal"]);
                    tampil["Jenis"] = Convert.ToString(baris["Jenis"]);
                    tampil["Keterangan"] = Convert.ToString(baris["Keterangan"]);
                    tampil["Masuk"] = qty > 0 ? qty : 0m;
                    tampil["Keluar"] = qty < 0 ? -qty : 0m;
                    tampil["Sisa"] = berjalan;
                    kartuTable.Rows.Add(tampil);
                }
                lblSaldoAwalKartu.Text = "Saldo Awal (" + dari + "): " + saldoAwal.ToString("N0");
                lblStokAkhirKartu.Text = "Stok Akhir: " + stokAkhir.ToString("N0");
                printItemKartu = Convert.ToString(cmbItemKartu.Text);
                printLokasiKartu = Convert.ToString(cmbLokasiKartu.Text);
                printPeriodeKartu = dari + " s/d " + TanggalSaja(dtpSampaiKartu.Value.Date);
                printSaldoAwalKartu = saldoAwal;
                printStokAkhirKartu = stokAkhir;
                printKartu = kartuTable.Copy();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menampilkan kartu stok:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCetakKartu_Click(object sender, EventArgs e)
        {
            if (printKartu == null || printKartu.Rows.Count == 0)
            {
                MessageBox.Show("Tampilkan kartu stok dulu!");
                return;
            }
            try
            {
                printIndexKartu = 0;
                docKartu = new PrintDocument();
                docKartu.DocumentName = "Kartu Stok " + printItemKartu;
                docKartu.PrintPage += DocKartu_PrintPage;
                PrintPreviewDialog preview = new PrintPreviewDialog();
                preview.Document = docKartu;
                preview.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mencetak kartu stok:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DocKartu_PrintPage(object sender, PrintPageEventArgs e)
        {
            Font judul = new Font("Segoe UI", 14, FontStyle.Bold);
            Font biasa = new Font("Segoe UI", 10);
            Font tebal = new Font("Segoe UI", 10, FontStyle.Bold);
            float x = e.MarginBounds.Left;
            float y = e.MarginBounds.Top;
            e.Graphics.DrawString("KARTU STOK", judul, Brushes.Black, x, y);
            y += 30;
            e.Graphics.DrawString("Barang: " + printItemKartu, biasa, Brushes.Black, x, y);
            y += 20;
            e.Graphics.DrawString("Lokasi: " + printLokasiKartu + "   Periode: " + printPeriodeKartu,
                biasa, Brushes.Black, x, y);
            y += 20;
            e.Graphics.DrawString("Saldo Awal: " + printSaldoAwalKartu.ToString("N0"),
                tebal, Brushes.Black, x, y);
            y += 30;
            e.Graphics.DrawString("Tanggal", tebal, Brushes.Black, x, y);
            e.Graphics.DrawString("Jenis", tebal, Brushes.Black, x + 150, y);
            e.Graphics.DrawString("Masuk", tebal, Brushes.Black, x + 330, y);
            e.Graphics.DrawString("Keluar", tebal, Brushes.Black, x + 420, y);
            e.Graphics.DrawString("Sisa", tebal, Brushes.Black, x + 510, y);
            y += 20;
            while (printIndexKartu < printKartu.Rows.Count)
            {
                DataRow baris = printKartu.Rows[printIndexKartu];
                e.Graphics.DrawString(Convert.ToString(baris["Tanggal"]), biasa, Brushes.Black, x, y);
                e.Graphics.DrawString(Convert.ToString(baris["Jenis"]), biasa, Brushes.Black, x + 150, y);
                e.Graphics.DrawString(Convert.ToDecimal(baris["Masuk"]).ToString("N0"), biasa, Brushes.Black, x + 330, y);
                e.Graphics.DrawString(Convert.ToDecimal(baris["Keluar"]).ToString("N0"), biasa, Brushes.Black, x + 420, y);
                e.Graphics.DrawString(Convert.ToDecimal(baris["Sisa"]).ToString("N0"), biasa, Brushes.Black, x + 510, y);
                y += 20;
                printIndexKartu++;
                if (y > e.MarginBounds.Bottom - 60)
                {
                    e.HasMorePages = true;
                    return;
                }
            }
            y += 10;
            e.Graphics.DrawString("Stok Akhir: " + printStokAkhirKartu.ToString("N0"),
                tebal, Brushes.Black, x, y);
            e.HasMorePages = false;
        }

        // ==================== PERAKITAN ====================
        private void LoadResep()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT r.ID_Rakitan, r.Nama || ' (' || i.Kode || ')' AS NamaTampil, " +
                        "r.ID_ItemJadi FROM Rakitan r INNER JOIN Item i " +
                        "ON i.ID_Item = r.ID_ItemJadi WHERE r.Aktif = 1 ORDER BY r.Nama", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        resepTable = new DataTable();
                        adapter.Fill(resepTable);
                        cmbResep.DisplayMember = "NamaTampil";
                        cmbResep.ValueMember = "ID_Rakitan";
                        cmbResep.DataSource = resepTable;
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Item, Nama, Kode FROM Item WHERE Tipe = 'Rakitan' ORDER BY Nama", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        itemJadiTable = new DataTable();
                        adapter.Fill(itemJadiTable);
                        cmbItemJadi.DisplayMember = "Nama";
                        cmbItemJadi.ValueMember = "ID_Item";
                        cmbItemJadi.DataSource = itemJadiTable;
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Item, Nama FROM Item WHERE Tipe IN ('Fisik','Rakitan') ORDER BY Nama", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        bahanTable = new DataTable();
                        adapter.Fill(bahanTable);
                        cmbBahanRakit.DisplayMember = "Nama";
                        cmbBahanRakit.ValueMember = "ID_Item";
                        cmbBahanRakit.DataSource = bahanTable;
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT r.ID_Rakitan, r.Nama || ' (' || i.Kode || ')' AS NamaTampil " +
                        "FROM Rakitan r INNER JOIN Item i ON i.ID_Item = r.ID_ItemJadi " +
                        "WHERE r.Aktif = 1 AND EXISTS (SELECT 1 FROM RakitanDetail d " +
                        "WHERE d.ID_Rakitan = r.ID_Rakitan) ORDER BY r.Nama", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        prosesRakitTable = new DataTable();
                        adapter.Fill(prosesRakitTable);
                        cmbProsesRakit.DisplayMember = "NamaTampil";
                        cmbProsesRakit.ValueMember = "ID_Rakitan";
                        cmbProsesRakit.DataSource = prosesRakitTable;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat resep rakitan:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cmbResep_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbResep.SelectedValue == null) return;
            MuatResepkeForm(Convert.ToInt32(cmbResep.SelectedValue));
        }

        private void MuatResepkeForm(int idRakitan)
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_ItemJadi FROM Rakitan WHERE ID_Rakitan = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idRakitan);
                        object hasil = cmd.ExecuteScalar();
                        if (hasil == null) return;
                        idRakitanPilihan = idRakitan;
                        cmbItemJadi.SelectedValue = Convert.ToInt32(hasil);
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT d.ID_RakitanDetail, d.ID_ItemBahan, i.Nama, d.Qty " +
                        "FROM RakitanDetail d INNER JOIN Item i " +
                        "ON i.ID_Item = d.ID_ItemBahan WHERE d.ID_Rakitan = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idRakitan);
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        resepDetailTable.Clear();
                        adapter.Fill(resepDetailTable);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat resep:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnResepBaru_Click(object sender, EventArgs e)
        {
            idRakitanPilihan = null;
            resepDetailTable.Clear();
            if (cmbItemJadi.Items.Count > 0)
                cmbItemJadi.SelectedIndex = 0;
        }

        private void btnTambahBahan_Click(object sender, EventArgs e)
        {
            if (cmbBahanRakit.SelectedValue == null)
            {
                MessageBox.Show("Pilih bahan dulu!");
                return;
            }
            decimal qty = numQtyBahan.Value;
            if (qty <= 0)
            {
                MessageBox.Show("Qty minimal 1!");
                return;
            }
            int idBahan = Convert.ToInt32(cmbBahanRakit.SelectedValue);
            string nama = Convert.ToString(cmbBahanRakit.Text);
            DataRow ada = null;
            foreach (DataRow baris in resepDetailTable.Rows)
            {
                if (Convert.ToInt32(baris["ID_ItemBahan"]) == idBahan) { ada = baris; break; }
            }
            if (ada != null)
            {
                ada["Qty"] = Convert.ToDecimal(ada["Qty"]) + qty;
            }
            else
            {
                DataRow baris = resepDetailTable.NewRow();
                baris["ID_RakitanDetail"] = 0;
                baris["ID_ItemBahan"] = idBahan;
                baris["Nama"] = nama;
                baris["Qty"] = qty;
                resepDetailTable.Rows.Add(baris);
            }
            numQtyBahan.Value = 1;
        }

        private void btnHapusBahan_Click(object sender, EventArgs e)
        {
            if (dgvResep.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih bahan di tabel dulu!");
                return;
            }
            resepDetailTable.Rows.RemoveAt(dgvResep.SelectedRows[0].Index);
        }

        private void dgvResep_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (MessageBox.Show("Hapus bahan ini dari resep?", "Konfirmasi",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                resepDetailTable.Rows.RemoveAt(e.RowIndex);
            }
        }

        private void btnSimpanResep_Click(object sender, EventArgs e)
        {
            if (cmbItemJadi.SelectedValue == null)
            {
                MessageBox.Show("Pilih barang jadi dulu!");
                return;
            }
            if (resepDetailTable.Rows.Count == 0)
            {
                MessageBox.Show("Tambahkan minimal 1 bahan dulu!");
                return;
            }
            int idJadi = Convert.ToInt32(cmbItemJadi.SelectedValue);
            string nama = Convert.ToString(cmbItemJadi.Text);
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        if (idRakitanPilihan == null)
                        {
                            using (var cmd = new SQLiteCommand(
                                "INSERT INTO Rakitan (ID_ItemJadi, Nama, Aktif) " +
                                "VALUES (@ID_ItemJadi, @Nama, 1)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID_ItemJadi", idJadi);
                                cmd.Parameters.AddWithValue("@Nama", nama);
                                cmd.ExecuteNonQuery();
                            }
                            using (var cmd = new SQLiteCommand("SELECT last_insert_rowid()", conn, trans))
                            {
                                idRakitanPilihan = (int)(long)cmd.ExecuteScalar();
                            }
                        }
                        else
                        {
                            using (var cmd = new SQLiteCommand(
                                "UPDATE Rakitan SET ID_ItemJadi = @ID_ItemJadi, Nama = @Nama " +
                                "WHERE ID_Rakitan = @ID", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID_ItemJadi", idJadi);
                                cmd.Parameters.AddWithValue("@Nama", nama);
                                cmd.Parameters.AddWithValue("@ID", idRakitanPilihan.Value);
                                cmd.ExecuteNonQuery();
                            }
                            using (var cmd = new SQLiteCommand(
                                "DELETE FROM RakitanDetail WHERE ID_Rakitan = @ID", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID", idRakitanPilihan.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        foreach (DataRow baris in resepDetailTable.Rows)
                        {
                            using (var cmd = new SQLiteCommand(
                                "INSERT INTO RakitanDetail (ID_Rakitan, ID_ItemBahan, Qty) " +
                                "VALUES (@ID_Rakitan, @ID_ItemBahan, @Qty)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID_Rakitan", idRakitanPilihan.Value);
                                cmd.Parameters.AddWithValue("@ID_ItemBahan", baris["ID_ItemBahan"]);
                                cmd.Parameters.AddWithValue("@Qty", baris["Qty"]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        trans.Commit();
                    }
                }
                LoadResep();
                MessageBox.Show("Resep tersimpan.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menyimpan resep:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnHapusResep_Click(object sender, EventArgs e)
        {
            if (idRakitanPilihan == null)
            {
                MessageBox.Show("Pilih resep dulu yang mau dihapus!");
                return;
            }
            if (MessageBox.Show("Hapus resep ini?", "Konfirmasi",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand(
                            "DELETE FROM RakitanDetail WHERE ID_Rakitan = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", idRakitanPilihan.Value);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand(
                            "DELETE FROM Rakitan WHERE ID_Rakitan = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", idRakitanPilihan.Value);
                            cmd.ExecuteNonQuery();
                        }
                        trans.Commit();
                    }
                }
                idRakitanPilihan = null;
                resepDetailTable.Clear();
                LoadResep();
                MessageBox.Show("Resep dihapus.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menghapus resep:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cmbProsesRakit_SelectedIndexChanged(object sender, EventArgs e)
        {
            PerbaruiInfoRakit();
        }

        private void numQtyRakit_ValueChanged(object sender, EventArgs e)
        {
            PerbaruiInfoRakit();
        }

        // Info bahan yang dibutuhkan untuk qty yang dipilih (read-only).
        private void PerbaruiInfoRakit()
        {
            lblInfoRakit.Text = "";
            if (cmbProsesRakit.SelectedValue == null) return;
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT i.Nama, d.Qty, " +
                        "COALESCE((SELECT Qty FROM StokLokasi WHERE ID_Lokasi = 1 " +
                        "AND ID_Item = d.ID_ItemBahan), 0) AS Stok " +
                        "FROM RakitanDetail d INNER JOIN Item i " +
                        "ON i.ID_Item = d.ID_ItemBahan " +
                        "WHERE d.ID_Rakitan = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", Convert.ToInt32(cmbProsesRakit.SelectedValue));
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            string info = "Bahan dibutuhkan: ";
                            bool pertama = true;
                            decimal qtyRakit = numQtyRakit.Value;
                            while (reader.Read())
                            {
                                decimal butuh = Convert.ToDecimal(reader["Qty"]) * qtyRakit;
                                decimal stok = Convert.ToDecimal(reader["Stok"]);
                                if (!pertama) info += ", ";
                                pertama = false;
                                info += Convert.ToString(reader["Nama"]) + " x" +
                                    butuh.ToString("N0") + " (stok " + stok.ToString("N0") + ")";
                            }
                            lblInfoRakit.Text = info;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lblInfoRakit.Text = "Gagal memuat info bahan: " + ex.Message;
            }
        }

        // Proses Rakit: SATU transaksi — bahan dicek & dikurangi (batch
        // dikonsumsi, HPP dijumlah), barang jadi bertambah + batch baru
        // seharga total HPP bahan / qty. Bahan kurang = ditolak dengan
        // pesan yang menyebut itemnya.
        private void btnProsesRakit_Click(object sender, EventArgs e)
        {
            if (cmbProsesRakit.SelectedValue == null)
            {
                MessageBox.Show("Pilih resep yang mau dirakit dulu!");
                return;
            }
            decimal qtyRakit = numQtyRakit.Value;
            if (qtyRakit <= 0)
            {
                MessageBox.Show("Qty minimal 1!");
                return;
            }
            int idRakitan = Convert.ToInt32(cmbProsesRakit.SelectedValue);
            try
            {
                int idJadi = 0;
                string namaJadi = "";
                DataTable bahan = new DataTable();
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_ItemJadi, Nama FROM Rakitan WHERE ID_Rakitan = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idRakitan);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show("Resep tidak ditemukan.");
                                return;
                            }
                            idJadi = Convert.ToInt32(reader["ID_ItemJadi"]);
                            namaJadi = Convert.ToString(reader["Nama"]);
                        }
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT d.ID_ItemBahan, i.Nama, d.Qty FROM RakitanDetail d " +
                        "INNER JOIN Item i ON i.ID_Item = d.ID_ItemBahan " +
                        "WHERE d.ID_Rakitan = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idRakitan);
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        adapter.Fill(bahan);
                    }
                    string tanggal = Sekarang();
                    string metodeHPP = "";
                    decimal totalHPPBahan = 0;
                    using (var trans = conn.BeginTransaction())
                    {
                        metodeHPP = StokHelper.GetHPPMethod(conn, trans);
                        foreach (DataRow baris in bahan.Rows)
                        {
                            int idBahan = Convert.ToInt32(baris["ID_ItemBahan"]);
                            string namaBahan = Convert.ToString(baris["Nama"]);
                            decimal butuh = Convert.ToDecimal(baris["Qty"]) * qtyRakit;
                            decimal stokBahan = StokHelper.StokLokasiQty(
                                conn, trans, idBahan, StokHelper.LokasiUtama);
                            if (butuh > stokBahan)
                            {
                                trans.Rollback();
                                MessageBox.Show("Bahan \"" + namaBahan + "\" tidak cukup " +
                                    "(butuh " + butuh.ToString("N0") + ", stok " +
                                    stokBahan.ToString("N0") + "). Perakitan dibatalkan.");
                                return;
                            }
                            StokHelper.HasilKonsumsi konsumsi = StokHelper.KonsumsiBatch(
                                conn, trans, idBahan, StokHelper.LokasiUtama,
                                butuh, metodeHPP);
                            totalHPPBahan += konsumsi.TotalHPP;
                            decimal? stokSesudah = StokHelper.TurunkanStok(
                                conn, trans, idBahan, StokHelper.LokasiUtama, butuh);
                            if (stokSesudah == null)
                            {
                                trans.Rollback();
                                MessageBox.Show("Bahan \"" + namaBahan +
                                    "\" tidak cukup (berubah saat memproses). Perakitan dibatalkan.");
                                return;
                            }
                            StokHelper.TulisMovement(conn, trans, idBahan,
                                StokHelper.LokasiUtama, tanggal, "Rakit Keluar",
                                -butuh, stokSesudah.Value, "RKT-" + idRakitan,
                                "Perakitan " + namaJadi);
                        }
                        decimal stokJadiSesudah = StokHelper.NaikkanStok(
                            conn, trans, idJadi, StokHelper.LokasiUtama, qtyRakit);
                        decimal hargaBeliJadi = qtyRakit != 0 ? totalHPPBahan / qtyRakit : 0m;
                        StokHelper.BuatBatch(conn, trans, idJadi,
                            StokHelper.LokasiUtama, tanggal, qtyRakit,
                            hargaBeliJadi, "Rakit", idRakitan);
                        StokHelper.TulisMovement(conn, trans, idJadi,
                            StokHelper.LokasiUtama, tanggal, "Rakit Masuk",
                            qtyRakit, stokJadiSesudah, "RKT-" + idRakitan,
                            "Perakitan " + namaJadi);
                        trans.Commit();
                    }
                }
                LoadResep();
                PerbaruiInfoRakit();
                MessageBox.Show("Perakitan selesai: " + namaJadi + " x" +
                    qtyRakit.ToString("N0") + " dibuat.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memproses rakitan:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==================== LOKASI ====================
        private void LoadLokasiSemua()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Lokasi, Kode, Nama, Alamat, IsAktif FROM Lokasi ORDER BY ID_Lokasi", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        lokasiTable = new DataTable();
                        adapter.Fill(lokasiTable);
                        dgvLokasi.DataSource = lokasiTable;
                        if (dgvLokasi.Columns["ID_Lokasi"] != null)
                            dgvLokasi.Columns["ID_Lokasi"].Visible = false;
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Lokasi, Nama FROM Lokasi WHERE IsAktif = 1 ORDER BY ID_Lokasi", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        lokasiAsalTable = new DataTable();
                        adapter.Fill(lokasiAsalTable);
                        cmbAsal.DisplayMember = "Nama";
                        cmbAsal.ValueMember = "ID_Lokasi";
                        cmbAsal.DataSource = lokasiAsalTable;
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Lokasi, Nama FROM Lokasi WHERE IsAktif = 1 ORDER BY ID_Lokasi", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        lokasiTujuanTable = new DataTable();
                        adapter.Fill(lokasiTujuanTable);
                        cmbTujuan.DisplayMember = "Nama";
                        cmbTujuan.ValueMember = "ID_Lokasi";
                        cmbTujuan.DataSource = lokasiTujuanTable;
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Lokasi, Nama FROM Lokasi WHERE IsAktif = 1 ORDER BY ID_Lokasi", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        lokasiOpnameTable = new DataTable();
                        adapter.Fill(lokasiOpnameTable);
                        cmbLokasiOpname.DisplayMember = "Nama";
                        cmbLokasiOpname.ValueMember = "ID_Lokasi";
                        cmbLokasiOpname.DataSource = lokasiOpnameTable;
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Item, Nama FROM Item WHERE Tipe IN ('Fisik','Rakitan') ORDER BY Nama", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        itemTransferTable = new DataTable();
                        adapter.Fill(itemTransferTable);
                        cmbItemTransfer.DisplayMember = "Nama";
                        cmbItemTransfer.ValueMember = "ID_Item";
                        cmbItemTransfer.DataSource = itemTransferTable;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat lokasi:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnTambahLokasi_Click(object sender, EventArgs e)
        {
            FormLokasi form = new FormLokasi();
            if (form.ShowDialog() != DialogResult.OK) return;
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "INSERT INTO Lokasi (Kode, Nama, Alamat, IsAktif) " +
                        "VALUES (@Kode, @Nama, @Alamat, 1)", conn))
                    {
                        cmd.Parameters.AddWithValue("@Kode", form.Kode);
                        cmd.Parameters.AddWithValue("@Nama", form.Nama);
                        cmd.Parameters.AddWithValue("@Alamat", form.Alamat);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadLokasiSemua();
                MessageBox.Show("Lokasi ditambahkan.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menambah lokasi (mungkin kode sudah dipakai):\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUbahLokasi_Click(object sender, EventArgs e)
        {
            if (dgvLokasi.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih lokasi dulu yang mau diubah!");
                return;
            }
            DataGridViewRow row = dgvLokasi.SelectedRows[0];
            int idLokasi = Convert.ToInt32(row.Cells["ID_Lokasi"].Value);
            FormLokasi form = new FormLokasi();
            form.IdLokasi = idLokasi;
            form.Kode = Convert.ToString(row.Cells["Kode"].Value);
            form.Nama = Convert.ToString(row.Cells["Nama"].Value);
            form.Alamat = Convert.ToString(row.Cells["Alamat"].Value);
            if (form.ShowDialog() != DialogResult.OK) return;
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "UPDATE Lokasi SET Kode = @Kode, Nama = @Nama, Alamat = @Alamat " +
                        "WHERE ID_Lokasi = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@Kode", form.Kode);
                        cmd.Parameters.AddWithValue("@Nama", form.Nama);
                        cmd.Parameters.AddWithValue("@Alamat", form.Alamat);
                        cmd.Parameters.AddWithValue("@ID", idLokasi);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadLokasiSemua();
                MessageBox.Show("Lokasi diupdate.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mengubah lokasi (mungkin kode sudah dipakai):\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==================== TRANSFER ====================
        private void LoadTransfer()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT t.ID_Transfer, t.NoTransfer, " +
                        "a.Nama AS Asal, b.Nama AS Tujuan, t.Tanggal, t.Status " +
                        "FROM TransferStok t " +
                        "LEFT JOIN Lokasi a ON a.ID_Lokasi = t.ID_LokasiAsal " +
                        "LEFT JOIN Lokasi b ON b.ID_Lokasi = t.ID_LokasiTujuan " +
                        "ORDER BY t.ID_Transfer DESC", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        transferTable = new DataTable();
                        adapter.Fill(transferTable);
                        dgvTransfer.DataSource = transferTable;
                        if (dgvTransfer.Columns["ID_Transfer"] != null)
                            dgvTransfer.Columns["ID_Transfer"].Visible = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat transfer:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnTambahItemTransfer_Click(object sender, EventArgs e)
        {
            if (idTransferPilihan != null && statusTransferPilihan != "Draft")
            {
                MessageBox.Show("Transfer ini sudah tidak bisa diubah (status " + statusTransferPilihan + ").");
                return;
            }
            if (cmbItemTransfer.SelectedValue == null)
            {
                MessageBox.Show("Pilih item dulu!");
                return;
            }
            decimal qty = numQtyTransfer.Value;
            if (qty <= 0)
            {
                MessageBox.Show("Qty minimal 1!");
                return;
            }
            int idItem = Convert.ToInt32(cmbItemTransfer.SelectedValue);
            string nama = Convert.ToString(cmbItemTransfer.Text);
            DataRow ada = null;
            foreach (DataRow baris in transferDetailTable.Rows)
            {
                if (Convert.ToInt32(baris["ID_Item"]) == idItem) { ada = baris; break; }
            }
            if (ada != null)
            {
                ada["Qty"] = Convert.ToDecimal(ada["Qty"]) + qty;
            }
            else
            {
                DataRow baris = transferDetailTable.NewRow();
                baris["ID_Item"] = idItem;
                baris["Nama"] = nama;
                baris["Qty"] = qty;
                transferDetailTable.Rows.Add(baris);
            }
            numQtyTransfer.Value = 1;
        }

        private void dgvTransferDetail_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (idTransferPilihan != null && statusTransferPilihan != "Draft") return;
            if (MessageBox.Show("Hapus baris ini dari transfer?", "Konfirmasi",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                transferDetailTable.Rows.RemoveAt(e.RowIndex);
            }
        }

        private void dgvTransfer_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = dgvTransfer.Rows[e.RowIndex];
            int id = Convert.ToInt32(row.Cells["ID_Transfer"].Value);
            MuatTransferkeForm(id);
        }

        private void MuatTransferkeForm(int idTransfer)
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_LokasiAsal, ID_LokasiTujuan, Catatan, Status " +
                        "FROM TransferStok WHERE ID_Transfer = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idTransfer);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read()) return;
                            idTransferPilihan = idTransfer;
                            statusTransferPilihan = Convert.ToString(reader["Status"]);
                            cmbAsal.SelectedValue = Convert.ToInt32(reader["ID_LokasiAsal"]);
                            cmbTujuan.SelectedValue = Convert.ToInt32(reader["ID_LokasiTujuan"]);
                            txtCatatanTransfer.Text = Convert.ToString(reader["Catatan"]);
                        }
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT d.ID_Item, i.Nama, d.Qty FROM TransferDetail d " +
                        "INNER JOIN Item i ON i.ID_Item = d.ID_Item " +
                        "WHERE d.ID_Transfer = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idTransfer);
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        transferDetailTable.Clear();
                        adapter.Fill(transferDetailTable);
                    }
                }
                PerbaruiTombolTransfer();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat transfer:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PerbaruiTombolTransfer()
        {
            if (idTransferPilihan == null)
            {
                btnSimpanTransfer.Enabled = true;
                btnSelesaikanTransfer.Enabled = false;
                btnBatalkanTransfer.Enabled = false;
                return;
            }
            switch (statusTransferPilihan)
            {
                case "Draft":
                    btnSimpanTransfer.Enabled = true;
                    btnSelesaikanTransfer.Enabled = true;
                    btnBatalkanTransfer.Enabled = true;
                    break;
                default: // Selesai / Dibatalkan
                    btnSimpanTransfer.Enabled = false;
                    btnSelesaikanTransfer.Enabled = false;
                    btnBatalkanTransfer.Enabled = false;
                    break;
            }
        }

        private void ResetFormTransfer()
        {
            idTransferPilihan = null;
            statusTransferPilihan = null;
            transferDetailTable.Clear();
            txtCatatanTransfer.Text = "";
            PerbaruiTombolTransfer();
        }

        private void btnBaruTransfer_Click(object sender, EventArgs e)
        {
            ResetFormTransfer();
        }

        private string BuatNoTransfer()
        {
            string tahun = DateTime.Now.ToString("yyyy");
            long jumlah = 0;
            using (var conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT COUNT(*) FROM TransferStok WHERE NoTransfer LIKE @Pola", conn))
                {
                    cmd.Parameters.AddWithValue("@Pola", "TRF-" + tahun + "-%");
                    jumlah = Convert.ToInt64(cmd.ExecuteScalar());
                }
            }
            return "TRF-" + tahun + "-" + (jumlah + 1).ToString("D4");
        }

        private void btnSimpanTransfer_Click(object sender, EventArgs e)
        {
            if (idTransferPilihan != null && statusTransferPilihan != "Draft")
            {
                MessageBox.Show("Transfer ini sudah tidak bisa diubah (status " + statusTransferPilihan + ").");
                return;
            }
            if (cmbAsal.SelectedValue == null || cmbTujuan.SelectedValue == null)
            {
                MessageBox.Show("Pilih lokasi asal dan tujuan dulu!");
                return;
            }
            int idAsal = Convert.ToInt32(cmbAsal.SelectedValue);
            int idTujuan = Convert.ToInt32(cmbTujuan.SelectedValue);
            if (idAsal == idTujuan)
            {
                MessageBox.Show("Lokasi asal dan tujuan tidak boleh sama!");
                return;
            }
            if (transferDetailTable.Rows.Count == 0)
            {
                MessageBox.Show("Tambahkan minimal 1 item dulu!");
                return;
            }
            string catatan = txtCatatanTransfer.Text.Trim();
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        if (idTransferPilihan == null)
                        {
                            using (var cmd = new SQLiteCommand(
                                "INSERT INTO TransferStok (NoTransfer, ID_LokasiAsal, " +
                                "ID_LokasiTujuan, Tanggal, Status, Catatan) " +
                                "VALUES (@NoTransfer, @Asal, @Tujuan, @Tanggal, " +
                                "'Draft', @Catatan)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@NoTransfer", BuatNoTransfer());
                                cmd.Parameters.AddWithValue("@Asal", idAsal);
                                cmd.Parameters.AddWithValue("@Tujuan", idTujuan);
                                cmd.Parameters.AddWithValue("@Tanggal", Sekarang());
                                cmd.Parameters.AddWithValue("@Catatan", catatan);
                                cmd.ExecuteNonQuery();
                            }
                            using (var cmd = new SQLiteCommand("SELECT last_insert_rowid()", conn, trans))
                            {
                                idTransferPilihan = (int)(long)cmd.ExecuteScalar();
                            }
                        }
                        else
                        {
                            using (var cmd = new SQLiteCommand(
                                "UPDATE TransferStok SET ID_LokasiAsal = @Asal, " +
                                "ID_LokasiTujuan = @Tujuan, Catatan = @Catatan " +
                                "WHERE ID_Transfer = @ID AND Status = 'Draft'", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@Asal", idAsal);
                                cmd.Parameters.AddWithValue("@Tujuan", idTujuan);
                                cmd.Parameters.AddWithValue("@Catatan", catatan);
                                cmd.Parameters.AddWithValue("@ID", idTransferPilihan.Value);
                                if (cmd.ExecuteNonQuery() == 0)
                                {
                                    trans.Rollback();
                                    MessageBox.Show("Transfer sudah tidak Draft — tidak bisa diubah.");
                                    return;
                                }
                            }
                            using (var cmd = new SQLiteCommand(
                                "DELETE FROM TransferDetail WHERE ID_Transfer = @ID", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID", idTransferPilihan.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        foreach (DataRow baris in transferDetailTable.Rows)
                        {
                            using (var cmd = new SQLiteCommand(
                                "INSERT INTO TransferDetail (ID_Transfer, ID_Item, Qty) " +
                                "VALUES (@ID_Transfer, @ID_Item, @Qty)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID_Transfer", idTransferPilihan.Value);
                                cmd.Parameters.AddWithValue("@ID_Item", baris["ID_Item"]);
                                cmd.Parameters.AddWithValue("@Qty", baris["Qty"]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        trans.Commit();
                    }
                }
                int idSimpan = idTransferPilihan.Value;
                LoadTransfer();
                MuatTransferkeForm(idSimpan);
                MessageBox.Show("Transfer tersimpan (Draft).");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menyimpan transfer:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Selesaikan transfer: SATU transaksi — stok asal dicek & dikurangi,
        // stok tujuan ditambah, total Item.Stok TIDAK berubah, batch dipindah
        // tertua dulu (HargaBeli + TanggalMasuk dipertahankan), log
        // 'Transfer Keluar' / 'Transfer Masuk' ditulis.
        private void btnSelesaikanTransfer_Click(object sender, EventArgs e)
        {
            if (idTransferPilihan == null) return;
            try
            {
                int idAsal = 0;
                int idTujuan = 0;
                string tanggal = Sekarang();
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_LokasiAsal, ID_LokasiTujuan FROM TransferStok " +
                        "WHERE ID_Transfer = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idTransferPilihan.Value);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show("Transfer tidak ditemukan.");
                                return;
                            }
                            idAsal = Convert.ToInt32(reader["ID_LokasiAsal"]);
                            idTujuan = Convert.ToInt32(reader["ID_LokasiTujuan"]);
                        }
                    }
                    using (var trans = conn.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand(
                            "UPDATE TransferStok SET Status = 'Selesai' " +
                            "WHERE ID_Transfer = @ID AND Status = 'Draft'", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", idTransferPilihan.Value);
                            if (cmd.ExecuteNonQuery() == 0)
                            {
                                trans.Rollback();
                                MessageBox.Show("Transfer sudah tidak Draft — " +
                                    "mungkin sudah diselesaikan.");
                                return;
                            }
                        }
                        DataTable detail = new DataTable();
                        using (var cmd = new SQLiteCommand(
                            "SELECT d.ID_Item, d.Qty, i.Nama FROM TransferDetail d " +
                            "INNER JOIN Item i ON i.ID_Item = d.ID_Item " +
                            "WHERE d.ID_Transfer = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", idTransferPilihan.Value);
                            SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                            adapter.Fill(detail);
                        }
                        foreach (DataRow baris in detail.Rows)
                        {
                            int idItem = Convert.ToInt32(baris["ID_Item"]);
                            decimal qty = Convert.ToDecimal(baris["Qty"]);
                            string namaItem = Convert.ToString(baris["Nama"]);
                            decimal stokAsal = StokHelper.StokLokasiQty(
                                conn, trans, idItem, idAsal);
                            if (qty > stokAsal)
                            {
                                trans.Rollback();
                                MessageBox.Show("Stok \"" + namaItem + "\" di lokasi asal " +
                                    "tidak cukup (tersisa " + stokAsal +
                                    "). Transfer dibatalkan.");
                                return;
                            }
                            StokHelper.PindahkanBatch(conn, trans, idItem,
                                idAsal, idTujuan, qty, idTransferPilihan.Value);
                            decimal? stokAsalSesudah = StokHelper.TurunkanStokLokasi(
                                conn, trans, idItem, idAsal, qty);
                            if (stokAsalSesudah == null)
                            {
                                trans.Rollback();
                                MessageBox.Show("Stok \"" + namaItem +
                                    "\" di lokasi asal tidak cukup (berubah saat " +
                                    "menyelesaikan). Transfer dibatalkan.");
                                return;
                            }
                            decimal stokTujuanSesudah = 0;
                            using (var cmd = new SQLiteCommand(
                                "UPDATE StokLokasi SET Qty = Qty + @Qty " +
                                "WHERE ID_Lokasi = @Lokasi AND ID_Item = @ID", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@Qty", qty);
                                cmd.Parameters.AddWithValue("@Lokasi", idTujuan);
                                cmd.Parameters.AddWithValue("@ID", idItem);
                                if (cmd.ExecuteNonQuery() == 0)
                                {
                                    using (var ins = new SQLiteCommand(
                                        "INSERT INTO StokLokasi (ID_Lokasi, ID_Item, Qty) " +
                                        "VALUES (@Lokasi, @ID, @Qty)", conn, trans))
                                    {
                                        ins.Parameters.AddWithValue("@Lokasi", idTujuan);
                                        ins.Parameters.AddWithValue("@ID", idItem);
                                        ins.Parameters.AddWithValue("@Qty", qty);
                                        ins.ExecuteNonQuery();
                                    }
                                }
                            }
                            stokTujuanSesudah = StokHelper.StokLokasiQty(
                                conn, trans, idItem, idTujuan);
                            StokHelper.TulisMovement(conn, trans, idItem,
                                idAsal, tanggal, "Transfer Keluar", -qty,
                                stokAsalSesudah.Value, "TRF-" + idTransferPilihan.Value,
                                "Transfer ke lokasi " + idTujuan);
                            StokHelper.TulisMovement(conn, trans, idItem,
                                idTujuan, tanggal, "Transfer Masuk", qty,
                                stokTujuanSesudah, "TRF-" + idTransferPilihan.Value,
                                "Transfer dari lokasi " + idAsal);
                        }
                        trans.Commit();
                    }
                }
                LoadTransfer();
                ResetFormTransfer();
                MessageBox.Show("Transfer selesai — stok pindah lokasi.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menyelesaikan transfer:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnBatalkanTransfer_Click(object sender, EventArgs e)
        {
            if (idTransferPilihan == null) return;
            if (MessageBox.Show("Batalkan transfer ini?", "Konfirmasi",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "UPDATE TransferStok SET Status = 'Dibatalkan' " +
                        "WHERE ID_Transfer = @ID AND Status = 'Draft'", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idTransferPilihan.Value);
                        if (cmd.ExecuteNonQuery() == 0)
                        {
                            MessageBox.Show("Transfer sudah tidak Draft — tidak bisa dibatalkan.");
                            return;
                        }
                    }
                }
                LoadTransfer();
                ResetFormTransfer();
                MessageBox.Show("Transfer dibatalkan.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal membatalkan transfer:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==================== OPNAME ====================
        private void LoadOpname()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT o.ID_Opname, o.NoOpname, l.Nama AS Lokasi, " +
                        "o.Tanggal, o.Status FROM Opname o " +
                        "LEFT JOIN Lokasi l ON l.ID_Lokasi = o.ID_Lokasi " +
                        "ORDER BY o.ID_Opname DESC", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        opnameTable = new DataTable();
                        adapter.Fill(opnameTable);
                        dgvOpname.DataSource = opnameTable;
                        if (dgvOpname.Columns["ID_Opname"] != null)
                            dgvOpname.Columns["ID_Opname"].Visible = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat opname:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvOpname_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = dgvOpname.Rows[e.RowIndex];
            int id = Convert.ToInt32(row.Cells["ID_Opname"].Value);
            MuatOpnamekeForm(id);
        }

        private void MuatOpnamekeForm(int idOpname)
        {
            try
            {
                memuatOpname = true;
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Lokasi, Status FROM Opname WHERE ID_Opname = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idOpname);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read()) return;
                            idOpnamePilihan = idOpname;
                            statusOpnamePilihan = Convert.ToString(reader["Status"]);
                            cmbLokasiOpname.SelectedValue = Convert.ToInt32(reader["ID_Lokasi"]);
                        }
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT d.ID_OpnameDetail, d.ID_Item, i.Kode, i.Nama, " +
                        "d.StokSistem, d.StokFisik, d.Selisih " +
                        "FROM OpnameDetail d INNER JOIN Item i " +
                        "ON i.ID_Item = d.ID_Item WHERE d.ID_Opname = @ID " +
                        "ORDER BY i.Nama", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idOpname);
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        opnameDetailTable.Clear();
                        adapter.Fill(opnameDetailTable);
                    }
                }
                AturEditOpnameDetail(statusOpnamePilihan == "Draft");
                PerbaruiTombolOpname();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat opname:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                memuatOpname = false;
            }
        }

        // Hanya kolom StokFisik yang bisa diedit (dan hanya untuk Draft).
        private void AturEditOpnameDetail(bool boleh)
        {
            if (dgvOpnameDetail.Columns["Kode"] != null)
                dgvOpnameDetail.Columns["Kode"].ReadOnly = true;
            if (dgvOpnameDetail.Columns["Nama"] != null)
                dgvOpnameDetail.Columns["Nama"].ReadOnly = true;
            if (dgvOpnameDetail.Columns["StokSistem"] != null)
                dgvOpnameDetail.Columns["StokSistem"].ReadOnly = true;
            if (dgvOpnameDetail.Columns["StokFisik"] != null)
                dgvOpnameDetail.Columns["StokFisik"].ReadOnly = !boleh;
            if (dgvOpnameDetail.Columns["Selisih"] != null)
                dgvOpnameDetail.Columns["Selisih"].ReadOnly = true;
        }

        private void PerbaruiTombolOpname()
        {
            bool draft = idOpnamePilihan != null && statusOpnamePilihan == "Draft";
            btnSimpanOpname.Enabled = draft;
            btnSelesaikanOpname.Enabled = draft;
            btnHapusOpname.Enabled = draft;
        }

        private void dgvOpnameDetail_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (memuatOpname || e.RowIndex < 0) return;
            if (dgvOpnameDetail.Columns[e.ColumnIndex].Name != "StokFisik") return;
            DataGridViewRow row = dgvOpnameDetail.Rows[e.RowIndex];
            decimal stokSistem = Convert.ToDecimal(row.Cells["StokSistem"].Value);
            decimal stokFisik = 0;
            try
            {
                stokFisik = Convert.ToDecimal(row.Cells["StokFisik"].Value);
            }
            catch
            {
                stokFisik = stokSistem;
                row.Cells["StokFisik"].Value = stokSistem;
            }
            if (stokFisik < 0)
            {
                stokFisik = 0;
                row.Cells["StokFisik"].Value = 0m;
            }
            row.Cells["Selisih"].Value = stokFisik - stokSistem;
        }

        private string BuatNoOpname()
        {
            string tahun = DateTime.Now.ToString("yyyy");
            long jumlah = 0;
            using (var conn = ConnectionManager.GetConnection())
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT COUNT(*) FROM Opname WHERE NoOpname LIKE @Pola", conn))
                {
                    cmd.Parameters.AddWithValue("@Pola", "OPN-" + tahun + "-%");
                    jumlah = Convert.ToInt64(cmd.ExecuteScalar());
                }
            }
            return "OPN-" + tahun + "-" + (jumlah + 1).ToString("D4");
        }

        // Buat opname baru (Draft): semua item Fisik/Rakitan di lokasi itu
        // dengan StokSistem dari StokLokasi dan StokFisik = StokSistem.
        private void btnBuatOpname_Click(object sender, EventArgs e)
        {
            if (cmbLokasiOpname.SelectedValue == null)
            {
                MessageBox.Show("Pilih lokasi dulu!");
                return;
            }
            int idLokasi = Convert.ToInt32(cmbLokasiOpname.SelectedValue);
            try
            {
                int idBaru = 0;
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO Opname (NoOpname, ID_Lokasi, Tanggal, Status, Catatan) " +
                            "VALUES (@NoOpname, @ID_Lokasi, @Tanggal, 'Draft', NULL)", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@NoOpname", BuatNoOpname());
                            cmd.Parameters.AddWithValue("@ID_Lokasi", idLokasi);
                            cmd.Parameters.AddWithValue("@Tanggal", Sekarang());
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand("SELECT last_insert_rowid()", conn, trans))
                        {
                            idBaru = (int)(long)cmd.ExecuteScalar();
                        }
                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO OpnameDetail (ID_Opname, ID_Item, StokSistem, " +
                            "StokFisik, Selisih) " +
                            "SELECT @ID_Opname, ID_Item, " +
                            "COALESCE((SELECT Qty FROM StokLokasi WHERE ID_Lokasi = @Lokasi " +
                            "AND ID_Item = Item.ID_Item), 0), " +
                            "COALESCE((SELECT Qty FROM StokLokasi WHERE ID_Lokasi = @Lokasi " +
                            "AND ID_Item = Item.ID_Item), 0), 0 " +
                            "FROM Item WHERE Tipe IN ('Fisik','Rakitan')", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID_Opname", idBaru);
                            cmd.Parameters.AddWithValue("@Lokasi", idLokasi);
                            cmd.ExecuteNonQuery();
                        }
                        trans.Commit();
                    }
                }
                LoadOpname();
                MuatOpnamekeForm(idBaru);
                MessageBox.Show("Opname dibuat (Draft). Isi kolom Stok Fisik, " +
                    "lalu Simpan Perubahan dan Selesaikan.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal membuat opname:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Simpan perubahan StokFisik dari grid ke OpnameDetail (Draft saja).
        // Mengembalikan true kalau berhasil. Dipakai tombol Simpan dan
        // tombol Selesaikan (supaya yang diselesaikan = angka yang tampil).
        private bool SimpanOpnameDetail()
        {
            if (idOpnamePilihan == null || statusOpnamePilihan != "Draft")
            {
                MessageBox.Show("Pilih opname Draft dulu!");
                return false;
            }
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        foreach (DataRow baris in opnameDetailTable.Rows)
                        {
                            using (var cmd = new SQLiteCommand(
                                "UPDATE OpnameDetail SET StokFisik = @StokFisik, " +
                                "Selisih = @Selisih WHERE ID_OpnameDetail = @ID", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@StokFisik", baris["StokFisik"]);
                                cmd.Parameters.AddWithValue("@Selisih", baris["Selisih"]);
                                cmd.Parameters.AddWithValue("@ID", baris["ID_OpnameDetail"]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        trans.Commit();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menyimpan opname:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void btnSimpanOpname_Click(object sender, EventArgs e)
        {
            if (SimpanOpnameDetail())
                MessageBox.Show("Perubahan opname tersimpan (masih Draft).");
        }

        // Selesaikan opname: SATU transaksi — per item yang selisihnya
        // bukan 0: StokLokasi = StokFisik, total Item.Stok disesuaikan,
        // kekurangan mengonsumsi batch, kelebihan membuat batch baru
        // seharga HargaBeli item, log 'Opname' ditulis.
        private void btnSelesaikanOpname_Click(object sender, EventArgs e)
        {
            if (idOpnamePilihan == null || statusOpnamePilihan != "Draft")
            {
                MessageBox.Show("Pilih opname Draft dulu!");
                return;
            }
            // Simpan dulu perubahan yang masih di grid (supaya yang
            // diselesaikan = angka yang tampil). Kalau gagal simpan,
            // berhenti di sini.
            if (!SimpanOpnameDetail()) return;
            try
            {
                int idLokasi = 0;
                string tanggal = Sekarang();
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Lokasi FROM Opname WHERE ID_Opname = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idOpnamePilihan.Value);
                        idLokasi = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    using (var trans = conn.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand(
                            "UPDATE Opname SET Status = 'Selesai' " +
                            "WHERE ID_Opname = @ID AND Status = 'Draft'", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", idOpnamePilihan.Value);
                            if (cmd.ExecuteNonQuery() == 0)
                            {
                                trans.Rollback();
                                MessageBox.Show("Opname sudah tidak Draft — " +
                                    "mungkin sudah diselesaikan.");
                                return;
                            }
                        }
                        DataTable detail = new DataTable();
                        using (var cmd = new SQLiteCommand(
                            "SELECT d.ID_Item, d.StokSistem, d.StokFisik, d.Selisih, " +
                            "i.HargaBeli, i.Nama FROM OpnameDetail d " +
                            "INNER JOIN Item i ON i.ID_Item = d.ID_Item " +
                            "WHERE d.ID_Opname = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", idOpnamePilihan.Value);
                            SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                            adapter.Fill(detail);
                        }
                        string metodeHPP = StokHelper.GetHPPMethod(conn, trans);
                        // Fase 4: nilai penyesuaian untuk jurnal opname.
                        // Surplus dinilai HargaBeli item; shortfall dinilai
                        // HPP batch yang terpakai (bukan HargaBeli saat ini).
                        decimal nilaiSurplus = 0;
                        decimal nilaiShortfall = 0;
                        foreach (DataRow baris in detail.Rows)
                        {
                            decimal selisih = Convert.ToDecimal(baris["Selisih"]);
                            if (selisih == 0) continue;
                            int idItem = Convert.ToInt32(baris["ID_Item"]);
                            decimal stokFisik = Convert.ToDecimal(baris["StokFisik"]);
                            decimal hargaBeli = Convert.ToDecimal(baris["HargaBeli"]);
                            StokHelper.SetStokLokasi(conn, trans, idItem, idLokasi, stokFisik);
                            using (var cmd = new SQLiteCommand(
                                "UPDATE Item SET Stok = Stok + @Selisih WHERE ID_Item = @ID",
                                conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@Selisih", selisih);
                                cmd.Parameters.AddWithValue("@ID", idItem);
                                cmd.ExecuteNonQuery();
                            }
                            if (selisih < 0)
                            {
                                StokHelper.HasilKonsumsi hasilKonsumsi =
                                    StokHelper.KonsumsiBatch(conn, trans, idItem,
                                        idLokasi, -selisih, metodeHPP);
                                nilaiShortfall += hasilKonsumsi.TotalHPP;
                            }
                            else
                            {
                                nilaiSurplus += selisih * hargaBeli;
                                StokHelper.BuatBatch(conn, trans, idItem,
                                    idLokasi, tanggal, selisih, hargaBeli,
                                    "Opname", idOpnamePilihan.Value);
                            }
                            StokHelper.TulisMovement(conn, trans, idItem,
                                idLokasi, tanggal, "Opname", selisih,
                                stokFisik, "OPN-" + idOpnamePilihan.Value,
                                "Stok opname");
                        }
                        // Fase 4: jurnal opname (dalam transaksi yang sama;
                        // kalau keduanya 0, helper tidak membuat jurnal).
                        // Surplus: Persediaan lawan Pendapatan Lain-lain.
                        // Shortfall: Biaya Selisih Persediaan lawan Persediaan.
                        JurnalHelper.TulisJurnalOpname(conn, trans, tanggal,
                            idOpnamePilihan.Value, nilaiSurplus, nilaiShortfall);
                        trans.Commit();
                    }
                }
                LoadOpname();
                MuatOpnamekeForm(idOpnamePilihan.Value);
                MessageBox.Show("Opname selesai — stok sudah disesuaikan.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menyelesaikan opname:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnHapusOpname_Click(object sender, EventArgs e)
        {
            if (idOpnamePilihan == null || statusOpnamePilihan != "Draft")
            {
                MessageBox.Show("Pilih opname Draft dulu!");
                return;
            }
            if (MessageBox.Show("Hapus draft opname ini?", "Konfirmasi",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand(
                            "DELETE FROM OpnameDetail WHERE ID_Opname = @ID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", idOpnamePilihan.Value);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand(
                            "DELETE FROM Opname WHERE ID_Opname = @ID AND Status = 'Draft'",
                            conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", idOpnamePilihan.Value);
                            if (cmd.ExecuteNonQuery() == 0)
                            {
                                trans.Rollback();
                                MessageBox.Show("Opname sudah tidak Draft.");
                                return;
                            }
                        }
                        trans.Commit();
                    }
                }
                idOpnamePilihan = null;
                statusOpnamePilihan = null;
                opnameDetailTable.Clear();
                LoadOpname();
                PerbaruiTombolOpname();
                MessageBox.Show("Draft opname dihapus.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menghapus opname:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==================== HPP ====================
        private void LoadHPP()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT Nilai FROM Pengaturan WHERE Kunci = 'HPPMethod'", conn))
                    {
                        object hasil = cmd.ExecuteScalar();
                        string metode = hasil == null ? "FIFO" : Convert.ToString(hasil);
                        cmbHPPMethod.SelectedItem = metode == "LIFO" ? "LIFO" : "FIFO";
                    }
                    using (var cmd = new SQLiteCommand(
                        "SELECT i.Kode || ' - ' || i.Nama AS Item, l.Nama AS Lokasi, " +
                        "b.TanggalMasuk, b.QtyAwal, b.QtySisa, b.HargaBeli, b.RefType " +
                        "FROM StokBatch b INNER JOIN Item i ON i.ID_Item = b.ID_Item " +
                        "INNER JOIN Lokasi l ON l.ID_Lokasi = b.ID_Lokasi " +
                        "WHERE b.QtySisa > 0 ORDER BY b.ID_Item, b.TanggalMasuk", conn))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                        batchTable = new DataTable();
                        adapter.Fill(batchTable);
                        dgvBatch.DataSource = batchTable;
                        if (dgvBatch.Columns["QtyAwal"] != null)
                            dgvBatch.Columns["QtyAwal"].DefaultCellStyle.Format = "N0";
                        if (dgvBatch.Columns["QtySisa"] != null)
                            dgvBatch.Columns["QtySisa"].DefaultCellStyle.Format = "N0";
                        if (dgvBatch.Columns["HargaBeli"] != null)
                            dgvBatch.Columns["HargaBeli"].DefaultCellStyle.Format = "N0";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat data HPP:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSimpanHPP_Click(object sender, EventArgs e)
        {
            if (cmbHPPMethod.SelectedItem == null)
            {
                MessageBox.Show("Pilih metode dulu!");
                return;
            }
            string metode = Convert.ToString(cmbHPPMethod.SelectedItem);
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    StokHelper.SetHPPMethod(conn, metode);
                }
                MessageBox.Show("Metode HPP disimpan: " + metode + ".");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menyimpan metode HPP:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
