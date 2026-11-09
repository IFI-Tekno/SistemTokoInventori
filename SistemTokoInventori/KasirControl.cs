using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace SistemTokoInventori
{
    /// <summary>
    /// Kasir sebagai UserControl (Fase 2): di-host FormMain di panel konten,
    /// bukan lagi Form modal. Logika dipertahankan dari FormKasir (Guna):
    /// keranjang, CekStokCukup, FormBayar, transaksi dua langkah B1 dengan
    /// flag stokDikurangi, FormStruk, Bayar/Kembalian tersimpan.
    ///
    /// Perubahan skema (v2, satu database): Produk -&gt; Item, ID_Produk -&gt;
    /// ID_Item, Harga -&gt; HargaJual. Tipe 'Jasa' dan 'NonInventori' tidak
    /// pernah mengurangi stok (tidak ada pelacakan stok); 'Rakitan' untuk
    /// sekarang berperilaku seperti 'Fisik' di POS (stok dikelola fitur
    /// Assembly di Fase 3). Setiap penjualan item stok juga menulis satu
    /// baris StokMovement (Jenis 'Jual').
    ///
    /// Fase 2: pelanggan (opsional) + diskon periode aktif otomatis
    /// (persen tertinggi, hanya kalau pelanggan dipilih) + poin (dapat
    /// floor((subtotal-diskon)/10000), pakai 1 poin = Rp 100, maks saldo
    /// dan maks total setelah diskon) + tukar tambah (nilai dikurangi dari
    /// total, barang masuk menaikkan stok) + pembayaran Tempo (butuh
    /// pelanggan, sisa jadi Piutang) + muat Sales Order ke keranjang.
    /// Semua perhitungan baru terjadi SEBELUM alur dua langkah B1;
    /// langkah 1 dan 2 sendiri tidak berubah kecuali langkah 2 kini
    /// menyimpan Diskon/PoinDipakai/PoinDidapat, piutang, dan tukar tambah
    /// dalam transaksi yang sama.
    ///
    /// Fase 3: penjualan memakai stok lokasi 1 (gudang utama) — langkah 1
    /// mengurangi StokLokasi + mengonsumsi batch (HPP FIFO/LIFO) dalam
    /// transaksi yang sama; HPP per baris disimpan di
    /// Detail_Transaksi.HPP; StokSesudah di log = stok lokasi 1.
    /// KembalikanStok mengembalikan Item.Stok + StokLokasi + batch.
    ///
    /// Fase 4: deposit pelanggan bisa dipakai sebagai cara bayar
    /// (dibatasi saldo deposit + sisa tagihan setelah diskon/poin/
    /// tukar; dipakai FIFO dari deposit tertua, dicatat di
    /// DepositPakai). Langkah 2 juga menulis jurnal penjualan via
    /// JurnalHelper (di transaksi yang sama — jurnal tidak seimbang
    /// = langkah 2 rollback, lalu stok dikembalikan seperti B1).
    ///
    /// Catatan: walau sekarang satu database, langkah 1 (kurangi stok) dan
    /// langkah 2 (simpan transaksi) sengaja tetap dua transaksi terpisah
    /// supaya logika kompensasi B1 tidak berubah.
    /// </summary>
    public partial class KasirControl : UserControl
    {
        private DataTable cartTable;

        // Tabel item yang tampil di grid — disimpan supaya filter pencarian
        // (RowFilter) bisa diterapkan tanpa query ulang ke database.
        private DataTable itemTable;

        // ----- Fase 2: pelanggan, diskon, poin, tukar tambah, SO -----
        // null = "Umum" (tanpa pelanggan): tanpa diskon periode, tanpa poin,
        // tanpa Tempo.
        private int? idPelanggan;
        private int poinPelanggan;
        private decimal persenDiskonAktif;
        private string namaDiskonAktif;
        // Tukar tambah yang sedang aktif untuk keranjang ini (0 = tidak ada).
        private decimal nilaiTukar;
        private string deskripsiTukar = "";
        private int? idItemMasukTukar;
        private decimal qtyMasukTukar;
        // Sales Order yang sedang dimuat ke keranjang (null = bukan dari SO).
        private int? idSalesOrderAktif;
        // Hasil perhitungan terakhir (dipakai btnProses + FormStruk).
        private decimal subtotalKeranjang;
        private decimal diskonKeranjang;
        private decimal nilaiPoinKeranjang;
        private decimal nilaiTukarEfektif;
        private decimal totalAkhir;
        private int poinDidapatKeranjang;
        // Fase 4: deposit pelanggan — total Sisa deposit Aktif pelanggan
        // yang dipilih, dan jumlah deposit yang dipakai di keranjang ini.
        private decimal depositSaldoPelanggan;
        private decimal depositDipakaiKeranjang;

        public KasirControl()
        {
            InitializeComponent();
            TerapkanTema();
            InitializeCart();
            LoadProduk();
            LoadPelangganCombo();
        }

        /// <summary>Dipanggil FormMain setiap kali tab Kasir dibuka.</summary>
        public void RefreshData()
        {
            LoadProduk();
            LoadPelangganCombo();
        }

        /// <summary>True kalau keranjang kosong (dipakai "Muat ke Kasir" SO).</summary>
        public bool KeranjangKosong
        {
            get { return cartTable.Rows.Count == 0; }
        }

        /// <summary>
        /// Pilih pelanggan dari kode (dipakai PenjualanControl saat SO dimuat
        /// ke Kasir). null = Umum. Kalau ID tidak ada di combo (mis. pelanggan
        /// sudah dihapus), jatuh ke Umum supaya diskon/poin tidak nempel ke
        /// pelanggan lain yang sedang terpilih.
        /// </summary>
        public void SetPelanggan(int? id)
        {
            if (id == null)
            {
                cmbPelanggan.SelectedValue = 0;
                return;
            }
            foreach (DataRowView baris in cmbPelanggan.Items)
            {
                if (Convert.ToInt32(baris["ID_Pelanggan"]) == id.Value)
                {
                    cmbPelanggan.SelectedValue = id.Value;
                    return;
                }
            }
            cmbPelanggan.SelectedValue = 0;
        }

        /// <summary>
        /// Muat isi Sales Order ke keranjang (dipanggil PenjualanControl,
        /// HANYA kalau keranjang kosong — pengecekan itu ada di sana).
        /// Harga per item = HargaJual - DiskonItem (diskon per item SO),
        /// disimpan di kolom DiskonItem supaya Detail_Transaksi dan struk
        /// tetap konsisten. Setelah ini SO berstatus 'Diproses' (diubah
        /// PenjualanControl); kalau transaksi Kasir selesai, SO jadi
        /// 'Selesai' (di langkah 2 btnProses). Kalau keranjang dikosongkan
        /// sebelum transaksi selesai, SO tetap 'Diproses' dan bisa dimuat
        /// ulang — idSalesOrderAktif direset saat keranjang kosong.
        /// </summary>
        public void MuatSalesOrder(int idSO, int? idPelangganSO, DataTable detail)
        {
            foreach (DataRow baris in detail.Rows)
            {
                DataRow cartRow = cartTable.NewRow();
                cartRow["ID_Item"] = Convert.ToInt32(baris["ID_Item"]);
                cartRow["Nama"] = Convert.ToString(baris["Nama"]);
                cartRow["HargaJual"] = Convert.ToDecimal(baris["HargaJual"]);
                cartRow["DiskonItem"] = Convert.ToDecimal(baris["DiskonItem"]);
                cartRow["Qty"] = Convert.ToInt32(baris["Qty"]);
                cartRow["Subtotal"] = (Convert.ToDecimal(baris["HargaJual"])
                    - Convert.ToDecimal(baris["DiskonItem"]))
                    * Convert.ToInt32(baris["Qty"]);
                cartRow["Tipe"] = Convert.ToString(baris["Tipe"]);
                cartTable.Rows.Add(cartRow);
            }
            idSalesOrderAktif = idSO;
            SetPelanggan(idPelangganSO);
            HitungTotal();
        }

        private void TerapkanTema()
        {
            this.BackColor = Theme.Background;
            this.Font = Theme.Font(10);
            Theme.StyleGrid(dgvProduk);
            Theme.StyleGrid(dgvCart);
            Theme.StylePrimaryButton(btnTambahCart);
            Theme.StyleSuccessButton(btnProses);
            btnProses.Height = 60;
            Theme.StyleGhostButton(btnTukarTambah);
            btnTukarTambah.Height = 60;
            Theme.StyleNumericUpDown(numQty);
            Theme.StyleNumericUpDown(numPoinDipakai);
            Theme.StyleNumericUpDown(numDepositDipakai);
            Theme.StyleTextBox(txtCari);
            Theme.StyleComboBox(cmbPelanggan);
            label1.ForeColor = Theme.Text;
            label2.ForeColor = Theme.Text;
            lblQty.ForeColor = Theme.Text;
            lblPelanggan.ForeColor = Theme.Text;
            lblPoinSaldo.ForeColor = Theme.Text;
            lblPoinDipakai.ForeColor = Theme.Text;
            lblDepositSaldo.ForeColor = Theme.Text;
            lblDepositDipakai.ForeColor = Theme.Text;
            lblDiskonAktif.ForeColor = Theme.SuccessDark;
            lblRingkasan.ForeColor = Theme.Muted;
            pnlTotal.FillColor = Theme.SuccessTint;
            lblTotal.ForeColor = Theme.SuccessDark;
            // Lihat Theme.BuildHeader: label transparan di atas Guna2Panel
            // mewarisi putih — beri BackColor opak = warna panel.
            lblTotal.BackColor = Theme.SuccessTint;
            var header = Theme.BuildHeader("Kasir", "Layani penjualan dan proses transaksi");
            this.Controls.Add(header);
        }

        // Tipe yang tidak melacak stok: tidak pernah dikurangi di POS.
        private static bool MelacakStok(string tipe)
        {
            return tipe != "Jasa" && tipe != "NonInventori";
        }

        private void InitializeCart()
        {
            cartTable = new DataTable();
            cartTable.Columns.Add("ID_Item", typeof(int));
            cartTable.Columns.Add("Nama", typeof(string));
            cartTable.Columns.Add("HargaJual", typeof(decimal));
            // Diskon per item (Rp) — 0 untuk item yang ditambah manual di
            // Kasir; terisi kalau keranjang dimuat dari Sales Order.
            // Subtotal = (HargaJual - DiskonItem) * Qty.
            cartTable.Columns.Add("DiskonItem", typeof(decimal));
            cartTable.Columns.Add("Qty", typeof(int));
            cartTable.Columns.Add("Subtotal", typeof(decimal));
            cartTable.Columns.Add("Tipe", typeof(string));
            dgvCart.DataSource = cartTable;
            if (dgvCart.Columns["ID_Item"] != null)
                dgvCart.Columns["ID_Item"].HeaderText = "ID";
            if (dgvCart.Columns["HargaJual"] != null)
                dgvCart.Columns["HargaJual"].HeaderText = "Harga";
            if (dgvCart.Columns["DiskonItem"] != null)
                dgvCart.Columns["DiskonItem"].Visible = false;
            if (dgvCart.Columns["Tipe"] != null)
                dgvCart.Columns["Tipe"].Visible = false;
        }

        // Combo pelanggan: baris pertama "Umum (tanpa pelanggan)" (ID 0),
        // lalu semua pelanggan dari database. Dipanggil ulang setiap
        // RefreshData supaya pelanggan baru dari Master Data muncul.
        private void LoadPelangganCombo()
        {
            try
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("ID_Pelanggan", typeof(int));
                dt.Columns.Add("Nama", typeof(string));
                DataRow umum = dt.NewRow();
                umum["ID_Pelanggan"] = 0;
                umum["Nama"] = "Umum (tanpa pelanggan)";
                dt.Rows.Add(umum);
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT ID_Pelanggan, Nama FROM Pelanggan ORDER BY Nama", conn))
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DataRow baris = dt.NewRow();
                            baris["ID_Pelanggan"] = Convert.ToInt32(reader["ID_Pelanggan"]);
                            baris["Nama"] = Convert.ToString(reader["Nama"]);
                            dt.Rows.Add(baris);
                        }
                    }
                }
                // Simpan pilihan lama supaya refresh tidak mereset pelanggan.
                int pilihanLama = idPelanggan.HasValue ? idPelanggan.Value : 0;
                cmbPelanggan.DisplayMember = "Nama";
                cmbPelanggan.ValueMember = "ID_Pelanggan";
                cmbPelanggan.DataSource = dt;
                SetPelanggan(pilihanLama == 0 ? (int?)null : pilihanLama);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat pelanggan:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cmbPelanggan_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbPelanggan.SelectedValue == null) return;
            PerbaruiPelanggan();
        }

        // Baca ID dari combo dengan aman: kalau SelectedValue masih
        // berupa DataRowView (misalnya ValueMember belum terpasang saat
        // event SelectedIndexChanged menyala ketika DataSource dipasang),
        // ambil kolomnya langsung dari baris itu. Tanpa ini,
        // Convert.ToInt32(DataRowView) melempar InvalidCastException
        // "Unable to cast object of type 'System.Data.DataRowView' to
        // type 'System.IConvertible'".
        private static int AmbilIdCombo(Guna.UI2.WinForms.Guna2ComboBox cmb, string kolom)
        {
            object nilai = cmb.SelectedValue;
            if (nilai is System.Data.DataRowView baris)
                return Convert.ToInt32(baris[kolom]);
            return Convert.ToInt32(nilai);
        }

        // Dipanggil setiap kali pelanggan berubah: muat saldo poin, atur
        // batas "Poin dipakai", lalu cari diskon periode aktif.
        private void PerbaruiPelanggan()
        {
            int id = AmbilIdCombo(cmbPelanggan, "ID_Pelanggan");
            idPelanggan = id == 0 ? (int?)null : id;
            if (idPelanggan == null)
            {
                poinPelanggan = 0;
                lblPoinSaldo.Text = "Poin: 0";
                // Urutan penting: Value diturunkan DULU sebelum Maximum,
                // supaya tidak ada sesaat di mana Value > Maximum.
                numPoinDipakai.Value = 0;
                numPoinDipakai.Maximum = 0;
                numPoinDipakai.Enabled = false;
                // Fase 4: tanpa pelanggan tidak ada deposit yang bisa dipakai.
                depositSaldoPelanggan = 0;
                depositDipakaiKeranjang = 0;
                lblDepositSaldo.Text = "Deposit: Rp 0";
                numDepositDipakai.Value = 0;
                numDepositDipakai.Maximum = 0;
                numDepositDipakai.Enabled = false;
                persenDiskonAktif = 0;
                namaDiskonAktif = null;
                lblDiskonAktif.Visible = false;
                HitungTotal();
                return;
            }
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT Poin FROM Pelanggan WHERE ID_Pelanggan = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idPelanggan.Value);
                        object hasil = cmd.ExecuteScalar();
                        poinPelanggan = hasil == null ? 0 : Convert.ToInt32(hasil);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat poin pelanggan:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                poinPelanggan = 0;
            }
            lblPoinSaldo.Text = "Poin: " + poinPelanggan.ToString("N0");
            // Urutan penting: Value diturunkan dulu kalau melebihi saldo
            // pelanggan baru, baru Maximum diubah.
            if (numPoinDipakai.Value > poinPelanggan)
                numPoinDipakai.Value = poinPelanggan;
            numPoinDipakai.Maximum = poinPelanggan;
            numPoinDipakai.Enabled = poinPelanggan > 0;
            // Fase 4: saldo deposit aktif pelanggan (dipakai di Kasir).
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT COALESCE(SUM(Sisa), 0) FROM Deposit " +
                        "WHERE ID_Pelanggan = @ID AND Status = 'Aktif'", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", idPelanggan.Value);
                        depositSaldoPelanggan = Convert.ToDecimal(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat deposit pelanggan:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                depositSaldoPelanggan = 0;
            }
            lblDepositSaldo.Text = "Deposit: Rp " + depositSaldoPelanggan.ToString("N0");
            // Urutan penting (sama seperti poin): Value diturunkan dulu
            // kalau melebihi saldo, baru Maximum diubah.
            if (numDepositDipakai.Value > depositSaldoPelanggan)
                numDepositDipakai.Value = depositSaldoPelanggan;
            numDepositDipakai.Maximum = depositSaldoPelanggan;
            numDepositDipakai.Enabled = depositSaldoPelanggan > 0;
            HitungDiskonAktif();
            HitungTotal();
        }

        // Diskon periode aktif (Aktif=1 dan hari ini di dalam rentang):
        // yang dipakai adalah Persen TERTINGGI. Hanya berlaku kalau ada
        // pelanggan yang dipilih (Umum = tanpa diskon).
        private void HitungDiskonAktif()
        {
            persenDiskonAktif = 0;
            namaDiskonAktif = null;
            lblDiskonAktif.Visible = false;
            if (idPelanggan == null) return;
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT Nama, Persen FROM DiskonPeriode " +
                        "WHERE Aktif = 1 AND TglMulai <= @Hari AND TglSelesai >= @Hari " +
                        "ORDER BY Persen DESC LIMIT 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@Hari",
                            DateTime.Now.ToString("yyyy-MM-dd"));
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                namaDiskonAktif = Convert.ToString(reader["Nama"]);
                                persenDiskonAktif = Convert.ToDecimal(reader["Persen"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat diskon aktif:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                persenDiskonAktif = 0;
                namaDiskonAktif = null;
            }
            if (persenDiskonAktif > 0)
            {
                lblDiskonAktif.Text = "Diskon " + namaDiskonAktif + " " +
                    persenDiskonAktif.ToString("0.##") + "% aktif";
                lblDiskonAktif.Visible = true;
            }
        }

        private void numPoinDipakai_ValueChanged(object sender, EventArgs e)
        {
            HitungTotal();
        }

        private void numDepositDipakai_ValueChanged(object sender, EventArgs e)
        {
            HitungTotal();
        }

        private void LoadProduk()
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    // Jasa / NonInventori tidak punya stok — tetap tampil
                    // walau Stok = 0, karena memang tidak dilacak.
                    string query = "SELECT ID_Item, Nama, HargaJual, Stok, Tipe FROM Item " +
                        "WHERE Stok > 0 OR Tipe IN ('Jasa', 'NonInventori') ORDER BY Nama";
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    itemTable = dt;
                    dgvProduk.DataSource = itemTable;
                    if (dgvProduk.Columns["HargaJual"] != null)
                    {
                        dgvProduk.Columns["HargaJual"].HeaderText = "Harga";
                        dgvProduk.Columns["HargaJual"].DefaultCellStyle.Format = "N0";
                    }
                    TerapkanFilterProduk();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat produk:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Pencarian: filter tabel yang sudah dimuat (tanpa query ulang).
        // Tanda ' digandakan dan wildcard LIKE (%, _, [) di-escape supaya
        // input apa pun tidak merusak sintaks RowFilter.
        private void TerapkanFilterProduk()
        {
            if (itemTable == null) return;
            string kata = txtCari.Text.Trim().Replace("'", "''");
            kata = kata.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
            itemTable.DefaultView.RowFilter = kata.Length == 0
                ? ""
                : "Nama LIKE '%" + kata + "%'";
        }

        private void txtCari_TextChanged(object sender, EventArgs e)
        {
            TerapkanFilterProduk();
        }

        private void btnTambahCart_Click(object sender, EventArgs e)
        {
            if (dgvProduk.SelectedRows.Count == 0)
            {
                MessageBox.Show("Pilih produk dulu!");
                return;
            }

            DataGridViewRow row = dgvProduk.SelectedRows[0];
            int idItem = Convert.ToInt32(row.Cells["ID_Item"].Value);
            string nama = row.Cells["Nama"].Value.ToString();
            decimal harga = Convert.ToDecimal(row.Cells["HargaJual"].Value);
            decimal stok = Convert.ToDecimal(row.Cells["Stok"].Value);
            string tipe = Convert.ToString(row.Cells["Tipe"].Value);
            int qty = (int)numQty.Value;

            // B3: qty 0 tidak boleh masuk keranjang — tanpa guard ini, qty 0
            // lolos (cek stok tidak menangkap 0) dan bisa diproses jadi
            // transaksi Rp 0. Minimum=1 di Designer adalah pertahanan pertama,
            // guard ini pertahanan kedua kalau nilainya diubah dari kode.
            if (qty <= 0)
            {
                MessageBox.Show("Qty minimal 1!");
                return;
            }

            // Stok di grid belum dikurangi isi keranjang, jadi qty yang sudah ada
            // di cart untuk item ini juga harus dihitung. Jasa / NonInventori
            // tidak dicek stoknya (tidak dilacak).
            if (MelacakStok(tipe))
            {
                int qtyDiCart = GetCartQty(idItem);
                if (qty + qtyDiCart > stok)
                {
                    MessageBox.Show("Stok tidak cukup!\n" +
                        "Stok tersisa: " + stok + "\n" +
                        "Sudah ada di keranjang: " + qtyDiCart);
                    return;
                }
            }

            // Item yang sama digabung menjadi satu baris keranjang.
            DataRow cartRow = CariBarisCart(idItem);
            if (cartRow != null)
            {
                cartRow["Qty"] = Convert.ToInt32(cartRow["Qty"]) + qty;
                cartRow["Subtotal"] = (harga - Convert.ToDecimal(cartRow["DiskonItem"]))
                    * Convert.ToInt32(cartRow["Qty"]);
            }
            else
            {
                cartRow = cartTable.NewRow();
                cartRow["ID_Item"] = idItem;
                cartRow["Nama"] = nama;
                cartRow["HargaJual"] = harga;
                cartRow["DiskonItem"] = 0m;
                cartRow["Qty"] = qty;
                cartRow["Subtotal"] = harga * qty;
                cartRow["Tipe"] = tipe;
                cartTable.Rows.Add(cartRow);
            }

            HitungTotal();
            numQty.Value = 1;
        }

        private int GetCartQty(int idItem)
        {
            foreach (DataRow row in cartTable.Rows)
            {
                if (Convert.ToInt32(row["ID_Item"]) == idItem)
                    return Convert.ToInt32(row["Qty"]);
            }
            return 0;
        }

        private DataRow CariBarisCart(int idItem)
        {
            foreach (DataRow row in cartTable.Rows)
            {
                if (Convert.ToInt32(row["ID_Item"]) == idItem)
                    return row;
            }
            return null;
        }

        private void dgvCart_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int idItem = Convert.ToInt32(dgvCart.Rows[e.RowIndex].Cells["ID_Item"].Value);
            DataRow baris = CariBarisCart(idItem);
            if (baris == null) return;
            if (MessageBox.Show("Hapus \"" + baris["Nama"] + "\" dari keranjang?", "Konfirmasi",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                cartTable.Rows.Remove(baris);
                HitungTotal();
            }
        }

        private decimal GetCartTotal()
        {
            decimal total = 0;
            foreach (DataRow row in cartTable.Rows)
            {
                total += Convert.ToDecimal(row["Subtotal"]);
            }
            return total;
        }

        // Urutan hitung (Fase 2):
        //   subtotal = jumlah Subtotal keranjang (sudah termasuk DiskonItem SO)
        //   diskon   = subtotal * persenDiskonAktif / 100 (diskon periode)
        //   poin     = "Poin dipakai" x Rp 100, dibatasi saldo pelanggan DAN
        //              total setelah diskon (tidak boleh membuat total negatif)
        //   tukar    = nilai tukar tambah, dibatasi sisa setelah diskon+poin
        //   deposit  = "Deposit dipakai", dibatasi saldo deposit DAN sisa
        //              setelah diskon+poin+tukar (Fase 4 — deposit adalah
        //              cara bayar seperti poin & tukar, bukan potongan harga)
        //   total    = subtotal - diskon - poin - tukar - deposit
        //   poinDidapat = floor((subtotal - diskon) / 10000), hanya kalau
        //              ada pelanggan. Poin dipakai & tukar tambah & deposit
        //              adalah cara bayar, bukan potongan harga — jadi tidak
        //              mengurangi dasar perolehan poin.
        // Dipanggil setiap kali keranjang / pelanggan / poin / tukar berubah.
        private void HitungTotal()
        {
            subtotalKeranjang = GetCartTotal();
            diskonKeranjang = subtotalKeranjang * persenDiskonAktif / 100m;

            // Keranjang kosong: SO yang dimuat & tukar tambah ikut batal.
            if (cartTable.Rows.Count == 0)
            {
                idSalesOrderAktif = null;
                nilaiTukar = 0;
                deskripsiTukar = "";
                idItemMasukTukar = null;
                qtyMasukTukar = 0;
            }

            int poinDipakai = (int)numPoinDipakai.Value;
            // Batas poin: saldo pelanggan DAN total setelah diskon.
            // (numPoinDipakai.Maximum sudah = saldo; batas total di sini.)
            int maksPoin = poinPelanggan;
            int maksDariTotal = (int)Math.Floor((subtotalKeranjang - diskonKeranjang) / 100m);
            if (maksDariTotal < maksPoin) maksPoin = maksDariTotal;
            if (maksPoin < 0) maksPoin = 0;
            if (poinDipakai > maksPoin)
            {
                numPoinDipakai.Value = maksPoin;
                poinDipakai = maksPoin;
            }
            nilaiPoinKeranjang = poinDipakai * 100m;

            nilaiTukarEfektif = nilaiTukar;
            decimal sisaSetelahPoin = subtotalKeranjang - diskonKeranjang - nilaiPoinKeranjang;
            if (nilaiTukarEfektif > sisaSetelahPoin) nilaiTukarEfektif = sisaSetelahPoin;
            if (nilaiTukarEfektif < 0) nilaiTukarEfektif = 0;

            // Fase 4: deposit dipakai — dibatasi saldo deposit pelanggan
            // DAN sisa yang masih harus dibayar setelah diskon, poin, dan
            // tukar tambah (supaya total tidak pernah negatif dan jurnal
            // penjualan tetap seimbang).
            decimal maksDeposit = depositSaldoPelanggan;
            decimal maksDariSisa = subtotalKeranjang - diskonKeranjang
                - nilaiPoinKeranjang - nilaiTukarEfektif;
            if (maksDariSisa < maksDeposit) maksDeposit = maksDariSisa;
            if (maksDeposit < 0) maksDeposit = 0;
            decimal depositDipakai = numDepositDipakai.Value;
            if (depositDipakai > maksDeposit)
            {
                numDepositDipakai.Value = maksDeposit;
                depositDipakai = maksDeposit;
            }
            depositDipakaiKeranjang = depositDipakai;

            totalAkhir = subtotalKeranjang - diskonKeranjang - nilaiPoinKeranjang - nilaiTukarEfektif - depositDipakaiKeranjang;
            if (totalAkhir < 0) totalAkhir = 0;

            poinDidapatKeranjang = idPelanggan.HasValue
                ? (int)Math.Floor((subtotalKeranjang - diskonKeranjang) / 10000m)
                : 0;
            if (poinDidapatKeranjang < 0) poinDidapatKeranjang = 0;

            string ringkasan = "Subtotal: Rp " + subtotalKeranjang.ToString("N0");
            if (diskonKeranjang > 0)
                ringkasan += "  •  Diskon -Rp " + diskonKeranjang.ToString("N0");
            if (nilaiPoinKeranjang > 0)
                ringkasan += "  •  Poin dipakai -Rp " + nilaiPoinKeranjang.ToString("N0");
            if (nilaiTukarEfektif > 0)
                ringkasan += "  •  Tukar tambah -Rp " + nilaiTukarEfektif.ToString("N0");
            if (depositDipakaiKeranjang > 0)
                ringkasan += "  •  Deposit dipakai -Rp " + depositDipakaiKeranjang.ToString("N0");
            if (poinDidapatKeranjang > 0)
                ringkasan += "  •  Poin didapat: " + poinDidapatKeranjang;
            lblRingkasan.Text = ringkasan;
            lblTotal.Text = "Total: Rp " + totalAkhir.ToString("N0");
        }

        // Tukar tambah: nilai barang lama mengurangi jumlah yang harus
        // dibayar (ditampilkan sebagai baris sendiri di ringkasan).
        // Kalau barang masuknya item yang sudah ada, stoknya naik saat
        // transaksi selesai (di langkah 2). Nilai tidak boleh melebihi
        // total setelah diskon & poin — batas itu dicek juga di modal.
        private void btnTukarTambah_Click(object sender, EventArgs e)
        {
            if (cartTable.Rows.Count == 0)
            {
                MessageBox.Show("Keranjang kosong!");
                return;
            }
            HitungTotal();
            decimal maksNilai = subtotalKeranjang - diskonKeranjang - nilaiPoinKeranjang;
            if (maksNilai <= 0)
            {
                MessageBox.Show("Tidak ada nilai yang bisa ditukar " +
                    "(total setelah diskon & poin sudah 0).");
                return;
            }
            FormTukarTambah form = new FormTukarTambah();
            form.MaxNilai = maksNilai;
            form.Deskripsi = deskripsiTukar;
            form.IdItemMasuk = idItemMasukTukar;
            form.QtyMasuk = qtyMasukTukar;
            form.NilaiTukar = nilaiTukar;
            if (form.ShowDialog() != DialogResult.OK)
                return;
            if (form.HapusDipilih)
            {
                nilaiTukar = 0;
                deskripsiTukar = "";
                idItemMasukTukar = null;
                qtyMasukTukar = 0;
            }
            else
            {
                nilaiTukar = form.NilaiTukar;
                deskripsiTukar = form.Deskripsi;
                idItemMasukTukar = form.IdItemMasuk;
                qtyMasukTukar = form.QtyMasuk;
            }
            HitungTotal();
        }

        private void btnProses_Click(object sender, EventArgs e)
        {
            if (cartTable.Rows.Count == 0)
            {
                MessageBox.Show("Keranjang kosong!");
                return;
            }

            // Salin isi keranjang dulu supaya aman kalau nanti di-clear.
            DataTable snapshot = cartTable.Copy();
            // Hitung ulang (diskon / poin / tukar) supaya nilainya final,
            // lalu capture ke variabel lokal — field direset setelah commit.
            HitungTotal();
            decimal subtotal = subtotalKeranjang;
            decimal diskon = diskonKeranjang;
            int poinDipakai = (int)numPoinDipakai.Value;
            int poinDidapat = poinDidapatKeranjang;
            decimal tukar = nilaiTukarEfektif;
            decimal depositDipakai = depositDipakaiKeranjang;
            decimal total = totalAkhir;

            // Cek stok dulu SEBELUM menerima uang: kalau stok sudah berubah
            // (mis. dibeli session lain), jangan sampai uang sudah diterima
            // tapi transaksi batal di langkah 1.
            if (!CekStokCukup(snapshot))
            {
                LoadProduk();
                return;
            }

            // Pembayaran (Tunai / Tempo). FormBayar memblokir konfirmasi
            // selama Bayar < Total (Tunai) atau tanpa pelanggan (Tempo),
            // jadi transaksi tidak mungkin tercatat dengan uang kurang.
            // Kalau dibatalkan, berhenti di sini — tidak ada stok yang
            // dikurangi, tidak ada transaksi tercatat. SO yang dimuat tetap
            // 'Diproses' dan bisa dimuat ulang.
            FormBayar formBayar = new FormBayar();
            formBayar.Total = total;
            formBayar.AdaPelanggan = idPelanggan.HasValue;
            if (formBayar.ShowDialog() != DialogResult.OK)
                return;
            decimal bayar = formBayar.Bayar;
            decimal kembalian = formBayar.Kembalian;
            string jenisBayar = formBayar.JenisBayar;
            DateTime jatuhTempo = formBayar.JatuhTempo;
            // Sisa piutang (Tempo) — dipakai di langkah 2 untuk Piutang
            // dan jurnal; dihitung di sini supaya nilainya final sebelum
            // transaksi disimpan.
            decimal sisaPiutang = jenisBayar == "Tempo" ? total - bayar : 0;
            if (sisaPiutang < 0) sisaPiutang = 0;

            // B1: stok baru dianggap "sudah dikurangi" SETELAH commit langkah 1
            // berhasil. Kalau exception terjadi sebelum itu (mis. database
            // terkunci saat mengurangi stok), stok TIDAK berkurang, jadi tidak
            // boleh dikembalikan — kalau dikembalikan, stok malah mengembang.
            bool stokDikurangi = false;
            long idTransaksi = 0;
            // Fase 3: rincian batch yang terpakai di langkah 1, per
            // ID_Item — untuk HPP Detail_Transaksi dan untuk
            // KembalikanStok kalau langkah 2 gagal.
            Dictionary<int, StokHelper.HasilKonsumsi> konsumsiStep1 =
                new Dictionary<int, StokHelper.HasilKonsumsi>();
            // Stok sesudah pengurangan, per ID_Item — untuk StokMovement.
            Dictionary<int, decimal> stokSesudah = new Dictionary<int, decimal>();

            try
            {
                // ===== 1. Kurangi stok di Item (dengan pengaman stok) =====
                // Hanya item yang melacak stok (Fisik / Rakitan). Jasa dan
                // NonInventori dilewati — tidak ada stok untuk dikurangi.
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        foreach (DataRow row in snapshot.Rows)
                        {
                            if (!MelacakStok(Convert.ToString(row["Tipe"])))
                                continue;
                            using (var cmd = new SQLiteCommand(
                                "UPDATE Item SET Stok = Stok - @Qty " +
                                "WHERE ID_Item = @ID AND Stok >= @Qty", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@Qty", row["Qty"]);
                                cmd.Parameters.AddWithValue("@ID", row["ID_Item"]);
                                int terubah = cmd.ExecuteNonQuery();
                                if (terubah == 0)
                                {
                                    trans.Rollback();
                                    MessageBox.Show("Stok \"" + row["Nama"] +
                                        "\" tidak cukup (mungkin sudah dibeli session lain). Transaksi dibatalkan.");
                                    LoadProduk();
                                    return;
                                }
                            }
                            // Fase 3: kurangi stok lokasi 1 (gudang utama)
                            // + konsumsi batch (HPP). StokSesudah di log =
                            // stok lokasi 1 sesudah pengurangan (kartu stok
                            // per lokasi memakai nilai ini). KonsumsiBatch
                            // melempar kalau batch tidak cukup — transaksi
                            // langkah 1 di-rollback saat dispose.
                            decimal? stokLokasiSesudah = StokHelper.TurunkanStokLokasi(
                                conn, trans, Convert.ToInt32(row["ID_Item"]),
                                StokHelper.LokasiUtama, Convert.ToDecimal(row["Qty"]));
                            if (stokLokasiSesudah == null)
                            {
                                trans.Rollback();
                                MessageBox.Show("Stok \"" + row["Nama"] +
                                    "\" di gudang utama tidak cukup. Transaksi dibatalkan.");
                                LoadProduk();
                                return;
                            }
                            stokSesudah[Convert.ToInt32(row["ID_Item"])] = stokLokasiSesudah.Value;
                            string metodeHPP = StokHelper.GetHPPMethod(conn, trans);
                            konsumsiStep1[Convert.ToInt32(row["ID_Item"])] =
                                StokHelper.KonsumsiBatch(conn, trans,
                                    Convert.ToInt32(row["ID_Item"]),
                                    StokHelper.LokasiUtama,
                                    Convert.ToDecimal(row["Qty"]), metodeHPP);
                        }
                        trans.Commit();
                    }
                }

                // Langkah 1 sudah commit: stok benar-benar berkurang.
                // Baru dari titik ini kompensasi di catch boleh jalan.
                stokDikurangi = true;

                // ===== 2. Simpan transaksi + detail + stok movement =====
                // Tanggal transaksi dipakai bersama oleh INSERT Transaksi
                // dan jurnal (supaya keduanya tercatat di tanggal yang sama).
                string tanggalTransaksi = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO Transaksi (Tanggal, Total, Bayar, Kembalian, " +
                            "ID_Pelanggan, Diskon, PoinDipakai, PoinDidapat) " +
                            "VALUES (@Tanggal, @Total, @Bayar, @Kembalian, " +
                            "@ID_Pelanggan, @Diskon, @PoinDipakai, @PoinDidapat)",
                            conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Tanggal", tanggalTransaksi);
                            cmd.Parameters.AddWithValue("@Total", total);
                            cmd.Parameters.AddWithValue("@Bayar", bayar);
                            cmd.Parameters.AddWithValue("@Kembalian", kembalian);
                            cmd.Parameters.AddWithValue("@ID_Pelanggan",
                                idPelanggan.HasValue ? (object)idPelanggan.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@Diskon", diskon);
                            cmd.Parameters.AddWithValue("@PoinDipakai", poinDipakai);
                            cmd.Parameters.AddWithValue("@PoinDidapat", poinDidapat);
                            cmd.ExecuteNonQuery();
                        }

                        using (var cmd = new SQLiteCommand("SELECT last_insert_rowid()", conn, trans))
                        {
                            idTransaksi = (long)cmd.ExecuteScalar();
                        }

                        foreach (DataRow row in snapshot.Rows)
                        {
                            using (var cmd = new SQLiteCommand(
                                "INSERT INTO Detail_Transaksi (ID_Transaksi, ID_Item, Qty, " +
                                "HargaSatuan, DiskonItem, Subtotal, HPP) " +
                                "VALUES (@ID_Transaksi, @ID_Item, @Qty, @HargaSatuan, @DiskonItem, @Subtotal, @HPP)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID_Transaksi", idTransaksi);
                                cmd.Parameters.AddWithValue("@ID_Item", row["ID_Item"]);
                                cmd.Parameters.AddWithValue("@Qty", row["Qty"]);
                                cmd.Parameters.AddWithValue("@HargaSatuan", row["HargaJual"]);
                                cmd.Parameters.AddWithValue("@DiskonItem", Convert.ToDecimal(row["DiskonItem"]));
                                cmd.Parameters.AddWithValue("@Subtotal", row["Subtotal"]);
                                // Fase 3: HPP baris ini (total HPP dari
                                // batch yang terpakai di langkah 1; 0 untuk
                                // Jasa / NonInventori yang tidak melacak stok).
                                int idDetail = Convert.ToInt32(row["ID_Item"]);
                                decimal hppBaris = konsumsiStep1.ContainsKey(idDetail)
                                    ? konsumsiStep1[idDetail].TotalHPP : 0m;
                                cmd.Parameters.AddWithValue("@HPP", hppBaris);
                                cmd.ExecuteNonQuery();
                            }

                            // Satu baris log stok per item terjual (Fase 1:
                            // kartu stok & FIFO di fase berikut memakai log ini).
                            if (MelacakStok(Convert.ToString(row["Tipe"])))
                            {
                                using (var cmd = new SQLiteCommand(
                                    "INSERT INTO StokMovement (ID_Item, Tanggal, Jenis, Qty, " +
                                    "StokSesudah, RefID, Keterangan, ID_Lokasi) " +
                                    "VALUES (@ID_Item, @Tanggal, 'Jual', @Qty, " +
                                    "@StokSesudah, @RefID, 'Penjualan', @Lokasi)", conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@ID_Item", row["ID_Item"]);
                                    cmd.Parameters.AddWithValue("@Tanggal", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                    cmd.Parameters.AddWithValue("@Qty", -Convert.ToDecimal(row["Qty"]));
                                    cmd.Parameters.AddWithValue("@StokSesudah", stokSesudah[Convert.ToInt32(row["ID_Item"])]);
                                    cmd.Parameters.AddWithValue("@RefID", idTransaksi.ToString());
                                    cmd.Parameters.AddWithValue("@Lokasi", StokHelper.LokasiUtama);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // ----- Fase 2: poin pelanggan -----
                        // Poin dipakai dikurangi, poin didapat ditambah —
                        // dalam transaksi yang sama supaya gagalnya transaksi
                        // juga membatalkan perubahan poin.
                        if (idPelanggan.HasValue)
                        {
                            using (var cmd = new SQLiteCommand(
                                "UPDATE Pelanggan SET Poin = Poin - @Dipakai + @Didapat " +
                                "WHERE ID_Pelanggan = @ID", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@Dipakai", poinDipakai);
                                cmd.Parameters.AddWithValue("@Didapat", poinDidapat);
                                cmd.Parameters.AddWithValue("@ID", idPelanggan.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // ----- Fase 2: tukar tambah -----
                        if (tukar > 0)
                        {
                            using (var cmd = new SQLiteCommand(
                                "INSERT INTO TukarTambah (ID_Transaksi, DeskripsiBarang, " +
                                "ID_ItemMasuk, QtyMasuk, NilaiTukar, Keterangan) " +
                                "VALUES (@ID_Transaksi, @Deskripsi, @ID_ItemMasuk, " +
                                "@QtyMasuk, @Nilai, NULL)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID_Transaksi", idTransaksi);
                                cmd.Parameters.AddWithValue("@Deskripsi", deskripsiTukar);
                                cmd.Parameters.AddWithValue("@ID_ItemMasuk",
                                    idItemMasukTukar.HasValue ? (object)idItemMasukTukar.Value : DBNull.Value);
                                cmd.Parameters.AddWithValue("@QtyMasuk", qtyMasukTukar);
                                cmd.Parameters.AddWithValue("@Nilai", tukar);
                                cmd.ExecuteNonQuery();
                            }
                            // Barang masuk (kalau dipilih item yang sudah ada)
                            // menaikkan stok + dicatat di StokMovement.
                            if (idItemMasukTukar.HasValue)
                            {
                                // Fase 3: stok naik di Item + StokLokasi
                                // (gudang utama) + batch baru. Harga beli
                                // batch = nilai tukar per unit (nilai yang
                                // diberikan ke pelanggan = biaya perolehan
                                // barang masuk).
                                decimal stokMasuk = StokHelper.NaikkanStok(
                                    conn, trans, idItemMasukTukar.Value,
                                    StokHelper.LokasiUtama, qtyMasukTukar);
                                decimal hargaBeliMasuk = qtyMasukTukar != 0
                                    ? tukar / qtyMasukTukar : 0m;
                                StokHelper.BuatBatch(conn, trans,
                                    idItemMasukTukar.Value, StokHelper.LokasiUtama,
                                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                    qtyMasukTukar, hargaBeliMasuk, "TukarTambah",
                                    idTransaksi);
                                using (var cmd = new SQLiteCommand(
                                    "INSERT INTO StokMovement (ID_Item, Tanggal, Jenis, Qty, " +
                                    "StokSesudah, RefID, Keterangan, ID_Lokasi) " +
                                    "VALUES (@ID_Item, @Tanggal, 'Masuk', @Qty, " +
                                    "@StokSesudah, @RefID, 'Tukar tambah', @Lokasi)", conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@ID_Item", idItemMasukTukar.Value);
                                    cmd.Parameters.AddWithValue("@Tanggal", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                    cmd.Parameters.AddWithValue("@Qty", qtyMasukTukar);
                                    cmd.Parameters.AddWithValue("@StokSesudah", stokMasuk);
                                    cmd.Parameters.AddWithValue("@RefID", idTransaksi.ToString());
                                    cmd.Parameters.AddWithValue("@Lokasi", StokHelper.LokasiUtama);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // ----- Fase 2: piutang (pembayaran Tempo) -----
                        // Sisa yang belum dibayar (Total - Bayar; Bayar boleh
                        // 0 / DP sebagian). Kalau lunas di tempat (sisa 0),
                        // tidak ada piutang yang dibuat.
                        if (jenisBayar == "Tempo" && total - bayar > 0)
                        {
                            using (var cmd = new SQLiteCommand(
                                "INSERT INTO Piutang (ID_Transaksi, NoFaktur, ID_Pelanggan, " +
                                "Tanggal, JatuhTempo, Total, Sisa, Status) " +
                                "VALUES (@ID_Transaksi, @NoFaktur, @ID_Pelanggan, " +
                                "@Tanggal, @JatuhTempo, @Total, @Sisa, 'Belum Lunas')",
                                conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID_Transaksi", idTransaksi);
                                cmd.Parameters.AddWithValue("@NoFaktur", "INV-" + idTransaksi);
                                cmd.Parameters.AddWithValue("@ID_Pelanggan", idPelanggan.Value);
                                cmd.Parameters.AddWithValue("@Tanggal", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                cmd.Parameters.AddWithValue("@JatuhTempo", jatuhTempo.ToString("yyyy-MM-dd"));
                                cmd.Parameters.AddWithValue("@Total", total);
                                cmd.Parameters.AddWithValue("@Sisa", total - bayar);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // ----- Fase 2: Sales Order selesai -----
                        // Transaksi Kasir dari SO yang dimuat selesai di sini.
                        // Kalau transaksi dibatalkan sebelumnya (FormBayar
                        // Cancel / stok gagal), kode ini tidak jalan dan SO
                        // tetap 'Diproses' — bisa dimuat ulang.
                        if (idSalesOrderAktif.HasValue)
                        {
                            using (var cmd = new SQLiteCommand(
                                "UPDATE SalesOrder SET Status = 'Selesai' WHERE ID_SO = @ID",
                                conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID", idSalesOrderAktif.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // ----- Fase 4: deposit dipakai (FIFO) -----
                        // Deposit tertua dipakai dulu. Kalau saldo deposit
                        // tidak cukup (seharusnya tidak terjadi karena
                        // HitungTotal sudah membatasi), lempar supaya
                        // langkah 2 di-rollback.
                        if (depositDipakai > 0)
                        {
                            decimal sisaDeposit = depositDipakai;
                            DataTable daftarDeposit = new DataTable();
                            using (var cmd = new SQLiteCommand(
                                "SELECT ID_Deposit, Sisa FROM Deposit " +
                                "WHERE ID_Pelanggan = @ID AND Status = 'Aktif' AND Sisa > 0 " +
                                "ORDER BY Tanggal ASC, ID_Deposit ASC", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@ID", idPelanggan.Value);
                                SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                                adapter.Fill(daftarDeposit);
                            }
                            foreach (DataRow barisDeposit in daftarDeposit.Rows)
                            {
                                if (sisaDeposit <= 0) break;
                                long idDeposit = Convert.ToInt64(barisDeposit["ID_Deposit"]);
                                decimal sisaBaris = Convert.ToDecimal(barisDeposit["Sisa"]);
                                decimal ambil = sisaBaris < sisaDeposit ? sisaBaris : sisaDeposit;
                                using (var cmd = new SQLiteCommand(
                                    "INSERT INTO DepositPakai (ID_Deposit, ID_Transaksi, " +
                                    "Tanggal, Jumlah) VALUES (@ID_Deposit, @ID_Transaksi, " +
                                    "@Tanggal, @Jumlah)", conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@ID_Deposit", idDeposit);
                                    cmd.Parameters.AddWithValue("@ID_Transaksi", idTransaksi);
                                    cmd.Parameters.AddWithValue("@Tanggal", tanggalTransaksi);
                                    cmd.Parameters.AddWithValue("@Jumlah", ambil);
                                    cmd.ExecuteNonQuery();
                                }
                                using (var cmd = new SQLiteCommand(
                                    "UPDATE Deposit SET Sisa = Sisa - @Jumlah, " +
                                    "Status = CASE WHEN Sisa - @Jumlah <= 0 THEN 'Terpakai' " +
                                    "ELSE 'Aktif' END " +
                                    "WHERE ID_Deposit = @ID AND Sisa >= @Jumlah", conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@Jumlah", ambil);
                                    cmd.Parameters.AddWithValue("@ID", idDeposit);
                                    if (cmd.ExecuteNonQuery() == 0)
                                    {
                                        throw new InvalidOperationException(
                                            "Saldo deposit berubah saat transaksi diproses.");
                                    }
                                }
                                sisaDeposit -= ambil;
                            }
                            if (sisaDeposit > 0)
                            {
                                throw new InvalidOperationException(
                                    "Saldo deposit tidak cukup (kurang Rp " +
                                    sisaDeposit.ToString("N0") + ").");
                            }
                        }

                        // ----- Fase 4: jurnal penjualan -----
                        // Ditulis di dalam transaksi langkah 2 yang sama:
                        // kalau jurnal tidak seimbang, helper melempar dan
                        // seluruh langkah 2 di-rollback (lalu stok
                        // dikembalikan seperti B1). HPP total = jumlah HPP
                        // semua baris yang terpakai di langkah 1. Kas masuk
                        // = Bayar - Kembalian (uang yang benar-benar diterima).
                        decimal totalHPP = 0;
                        foreach (KeyValuePair<int, StokHelper.HasilKonsumsi> kv in konsumsiStep1)
                        {
                            totalHPP += kv.Value.TotalHPP;
                        }
                        JurnalHelper.TulisJurnalPenjualan(conn, trans,
                            tanggalTransaksi, idTransaksi, subtotal - diskon,
                            bayar - kembalian, sisaPiutang, depositDipakai,
                            poinDipakai * 100m, tukar, totalHPP);

                        trans.Commit();
                    }
                }

                cartTable.Clear();
                // Reset state Fase 2 untuk pelanggan berikutnya. Poin direset
                // dulu, lalu pelanggan ke Umum (memicu PerbaruiPelanggan),
                // lalu HitungTotal final.
                idSalesOrderAktif = null;
                nilaiTukar = 0;
                deskripsiTukar = "";
                idItemMasukTukar = null;
                qtyMasukTukar = 0;
                numPoinDipakai.Value = 0;
                numDepositDipakai.Value = 0;
                cmbPelanggan.SelectedValue = 0;
                HitungTotal();
                LoadProduk();
                // Struk ditampilkan setelah transaksi tersimpan (bukan MessageBox).
                FormStruk formStruk = new FormStruk(idTransaksi, DateTime.Now,
                    snapshot, subtotal, diskon, poinDipakai * 100m, tukar,
                    depositDipakai, total, bayar, kembalian,
                    jenisBayar == "Tempo", sisaPiutang, jatuhTempo);
                formStruk.ShowDialog();
            }
            catch (Exception ex)
            {
                // Hanya kembalikan stok kalau langkah 1 memang sudah commit.
                // Kalau gagalnya di langkah 1, stok tidak berkurang sama sekali
                // (transaksi di-rollback), jadi mengembalikan stok justru
                // menambah stok dari udara.
                if (stokDikurangi)
                {
                    KembalikanStok(snapshot, konsumsiStep1);
                    MessageBox.Show("Transaksi gagal, stok dikembalikan.\n" + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Transaksi gagal, stok tidak berubah.\n" + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Cek cepat sebelum FormBayar dibuka: apakah stok di database masih
        // cukup untuk semua baris keranjang? Baris Jasa / NonInventori
        // dilewati (tidak ada stok). Kalau ada yang kurang (mis. stok
        // berubah sejak grid dimuat), tampilkan pesan dan batalkan — uang
        // pelanggan belum diterima, jadi tidak ada yang perlu dikembalikan.
        private bool CekStokCukup(DataTable snapshot)
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    foreach (DataRow row in snapshot.Rows)
                    {
                        if (!MelacakStok(Convert.ToString(row["Tipe"])))
                            continue;
                        // Fase 3: stok yang bisa dijual = stok di lokasi 1
                        // (gudang utama). Kalau belum ada baris StokLokasi
                        // (item lama), pakai Item.Stok sebagai fallback.
                        using (var cmd = new SQLiteCommand(
                            "SELECT COALESCE((SELECT Qty FROM StokLokasi " +
                            "WHERE ID_Lokasi = @Lokasi AND ID_Item = @ID), Stok) " +
                            "FROM Item WHERE ID_Item = @ID", conn))
                        {
                            cmd.Parameters.AddWithValue("@Lokasi", StokHelper.LokasiUtama);
                            cmd.Parameters.AddWithValue("@ID", row["ID_Item"]);
                            object hasil = cmd.ExecuteScalar();
                            if (hasil == null)
                            {
                                MessageBox.Show("Item \"" + row["Nama"] +
                                    "\" tidak ditemukan lagi. Transaksi dibatalkan.");
                                return false;
                            }
                            decimal stok = Convert.ToDecimal(hasil);
                            int qty = Convert.ToInt32(row["Qty"]);
                            if (qty > stok)
                            {
                                MessageBox.Show("Stok \"" + row["Nama"] +
                                    "\" tidak cukup (tersisa " + stok +
                                    "). Transaksi dibatalkan.");
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mengecek stok:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Kompensasi B1: kembalikan stok yang sudah dikurangi di langkah 1
        // (Item.Stok + StokLokasi + batch yang terpakai) kalau langkah 2
        // gagal. Satu transaksi untuk semua baris.
        private void KembalikanStok(DataTable snapshot,
            Dictionary<int, StokHelper.HasilKonsumsi> konsumsiPerItem)
        {
            try
            {
                using (var conn = ConnectionManager.GetConnection())
                {
                    conn.Open();
                    // Satu transaksi untuk semua baris: kalau ada yang gagal
                    // (mis. item sudah dihapus), semuanya di-rollback supaya
                    // stok tidak kembali setengah-setengah. Baris Jasa /
                    // NonInventori dilewati (stoknya memang tidak berkurang).
                    using (var trans = conn.BeginTransaction())
                    {
                        foreach (DataRow row in snapshot.Rows)
                        {
                            if (!MelacakStok(Convert.ToString(row["Tipe"])))
                                continue;
                            using (var cmd = new SQLiteCommand(
                                "UPDATE Item SET Stok = Stok + @Qty WHERE ID_Item = @ID",
                                conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@Qty", row["Qty"]);
                                cmd.Parameters.AddWithValue("@ID", row["ID_Item"]);
                                int terubah = cmd.ExecuteNonQuery();
                                if (terubah == 0)
                                {
                                    trans.Rollback();
                                    throw new InvalidOperationException(
                                        "Item ID " + row["ID_Item"] +
                                        " tidak ditemukan saat mengembalikan stok.");
                                }
                            }
                            // Fase 3: kembalikan juga stok lokasi + batch
                            // yang sudah terpakai di langkah 1.
                            StokHelper.NaikkanStokLokasi(conn, trans,
                                Convert.ToInt32(row["ID_Item"]),
                                StokHelper.LokasiUtama, Convert.ToDecimal(row["Qty"]));
                            if (konsumsiPerItem != null
                                && konsumsiPerItem.ContainsKey(Convert.ToInt32(row["ID_Item"])))
                            {
                                StokHelper.KembalikanBatch(conn, trans,
                                    konsumsiPerItem[Convert.ToInt32(row["ID_Item"])].Rincian);
                            }
                        }
                        trans.Commit();
                    }
                }
            }
            catch (Exception exKembali)
            {
                MessageBox.Show("PERINGATAN: gagal mengembalikan stok!\n" + exKembali.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
