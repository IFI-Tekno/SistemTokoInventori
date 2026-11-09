using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace SistemTokoInventori
{
    /// <summary>
    /// Tema visual terpusat (Layout A: sidebar + dashboard, palet biru).
    /// Semua warna dan gaya kontrol ada di sini, jadi mengubah tampilan
    /// cukup dari satu file — tidak perlu mengedit tiap Designer.
    /// Font: Segoe UI di semua tempat.
    /// Kontrol memakai Guna.UI2.WinForms (DLL dibundel di libs/, tanpa NuGet);
    /// helper di sini memanggil properti Guna2 (FillColor, BorderRadius,
    /// ThemeStyle, ...) — bukan FlatStyle seperti Button standar.
    /// </summary>
    public static class Theme
    {
        // ===== Palet (disetujui dari mockup) =====
        public static readonly Color Primary = Color.FromArgb(0x25, 0x63, 0xEB);      // #2563EB
        public static readonly Color PrimaryDark = Color.FromArgb(0x1D, 0x4E, 0xD8);  // #1D4ED8
        public static readonly Color Sidebar = Color.FromArgb(0x1E, 0x29, 0x3B);      // #1E293B
        public static readonly Color SidebarHover = Color.FromArgb(0x33, 0x41, 0x55); // #334155
        public static readonly Color SidebarText = Color.FromArgb(0xCB, 0xD5, 0xE1);  // #CBD5E1
        public static readonly Color SidebarMuted = Color.FromArgb(0x94, 0xA3, 0xB8); // #94A3B8
        public static readonly Color Background = Color.FromArgb(0xF1, 0xF5, 0xF9);   // #F1F5F9
        public static readonly Color Card = Color.White;
        public static readonly Color Text = Color.FromArgb(0x0F, 0x17, 0x2A);         // #0F172A
        public static readonly Color Muted = Color.FromArgb(0x64, 0x74, 0x8B);         // #64748B
        public static readonly Color Success = Color.FromArgb(0x16, 0xA3, 0x4A);      // #16A34A
        public static readonly Color SuccessTint = Color.FromArgb(0xDC, 0xFC, 0xE7);  // #DCFCE7
        public static readonly Color SuccessDark = Color.FromArgb(0x14, 0x53, 0x2D);  // #14532D
        public static readonly Color Danger = Color.FromArgb(0xDC, 0x26, 0x26);       // #DC2626
        public static readonly Color DangerDark = Color.FromArgb(0x7F, 0x1D, 0x1D);   // #7F1D1D (Fase 4: teks merah gelap, pasangan SuccessDark)
        public static readonly Color Warning = Color.FromArgb(0xD9, 0x77, 0x06);      // #D97706
        public static readonly Color Teal = Color.FromArgb(0x0D, 0x94, 0x88);         // #0D9488
        public static readonly Color GridLine = Color.FromArgb(0xE2, 0xE8, 0xF0);     // #E2E8F0
        public static readonly Color AltRow = Color.FromArgb(0xF8, 0xFA, 0xFC);        // #F8FAFC
        public static readonly Color HeaderSub = Color.FromArgb(0xDB, 0xEA, 0xFE);    // #DBEAFE

        public static Font Font(float size, FontStyle style = FontStyle.Regular)
        {
            return new Font("Segoe UI", size, style);
        }

        // ===== Tombol (Guna2Button) =====
        private static void StyleButtonBase(Guna2Button btn, Color fill, Color fore, Color hover)
        {
            btn.BorderRadius = 10;
            btn.FillColor = fill;
            btn.ForeColor = fore;
            btn.HoverState.FillColor = hover;
            btn.Font = Font(10, FontStyle.Bold);
            btn.Height = 42;
            btn.Cursor = Cursors.Hand;
        }

        public static void StylePrimaryButton(Guna2Button btn)
        {
            StyleButtonBase(btn, Primary, Color.White, PrimaryDark);
        }

        public static void StyleSuccessButton(Guna2Button btn)
        {
            StyleButtonBase(btn, Success, Color.White, Color.FromArgb(0x15, 0x8A, 0x3D));
        }

        public static void StyleDangerButton(Guna2Button btn)
        {
            StyleButtonBase(btn, Danger, Color.White, Color.FromArgb(0xB9, 0x1C, 0x1C));
        }

        public static void StyleGhostButton(Guna2Button btn)
        {
            StyleButtonBase(btn, Color.White, Text, AltRow);
            btn.BorderColor = Muted;
            btn.BorderThickness = 1;
        }

        /// <summary>Tombol navigasi di sidebar. active = halaman yang sedang dibuka.</summary>
        public static void StyleNavButton(Guna2Button btn, bool active)
        {
            btn.BorderRadius = 10;
            btn.FillColor = active ? Primary : Sidebar;
            btn.ForeColor = active ? Color.White : SidebarText;
            btn.HoverState.FillColor = active ? PrimaryDark : SidebarHover;
            btn.Font = Font(11, FontStyle.Bold);
            btn.TextAlign = HorizontalAlignment.Left;
            btn.Padding = new Padding(16, 0, 0, 0);
            btn.Height = 46;
            btn.Cursor = Cursors.Hand;
        }

        // ===== Grid (Guna2DataGridView) =====
        public static void StyleGrid(Guna2DataGridView grid)
        {
            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = GridLine;
            grid.RowHeadersVisible = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 36;
            grid.RowTemplate.Height = 30;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Sidebar;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = Font(10, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.DefaultCellStyle.Font = Font(10);
            grid.DefaultCellStyle.ForeColor = Text;
            grid.DefaultCellStyle.SelectionBackColor = Primary;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.AlternatingRowsDefaultCellStyle.BackColor = AltRow;
            // Tema Guna2 (di atas gaya standar): header biru gelap + baris selang-seling.
            grid.ThemeStyle.HeaderStyle.BackColor = Sidebar;
            grid.ThemeStyle.HeaderStyle.ForeColor = Color.White;
            grid.ThemeStyle.HeaderStyle.Font = Font(10, FontStyle.Bold);
            grid.ThemeStyle.RowsStyle.BackColor = Color.White;
            grid.ThemeStyle.RowsStyle.ForeColor = Text;
            grid.ThemeStyle.RowsStyle.Font = Font(10);
            grid.ThemeStyle.RowsStyle.SelectionBackColor = Primary;
            grid.ThemeStyle.RowsStyle.SelectionForeColor = Color.White;
            grid.ThemeStyle.AlternatingRowsStyle.BackColor = AltRow;
            grid.ThemeStyle.AlternatingRowsStyle.ForeColor = Text;
            grid.ThemeStyle.AlternatingRowsStyle.Font = Font(10);
            grid.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = Primary;
            grid.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = Color.White;
        }

        // ===== Input (Guna2TextBox / Guna2ComboBox / Guna2NumericUpDown) =====
        public static void StyleTextBox(Guna2TextBox txt)
        {
            txt.BorderRadius = 8;
            txt.BorderColor = GridLine;
            txt.FillColor = Color.White;
            txt.Font = Font(10);
            txt.FocusedState.BorderColor = Primary;
        }

        public static void StyleComboBox(Guna2ComboBox cmb)
        {
            cmb.BorderRadius = 8;
            cmb.BorderColor = GridLine;
            cmb.FillColor = Color.White;
            cmb.Font = Font(10);
            cmb.FocusedState.BorderColor = Primary;
        }

        public static void StyleNumericUpDown(Guna2NumericUpDown num)
        {
            num.BorderRadius = 8;
            num.BorderColor = GridLine;
            num.FillColor = Color.White;
            num.Font = Font(10);
            num.UpDownButtonFillColor = Primary;
        }

        // ===== Tab (Guna2TabControl) =====
        public static void StyleTabControl(Guna2TabControl tab)
        {
            tab.TabMenuBackColor = Sidebar;
            tab.TabButtonIdleState.FillColor = Sidebar;
            tab.TabButtonIdleState.ForeColor = SidebarText;
            tab.TabButtonIdleState.Font = Font(10, FontStyle.Bold);
            tab.TabButtonHoverState.FillColor = SidebarHover;
            tab.TabButtonHoverState.ForeColor = Color.White;
            tab.TabButtonHoverState.Font = Font(10, FontStyle.Bold);
            tab.TabButtonSelectedState.FillColor = Primary;
            tab.TabButtonSelectedState.ForeColor = Color.White;
            tab.TabButtonSelectedState.Font = Font(10, FontStyle.Bold);
        }

        // ===== Date picker (Guna2DateTimePicker) =====
        public static void StyleDatePicker(Guna2DateTimePicker dtp)
        {
            dtp.BorderRadius = 8;
            dtp.BorderColor = GridLine;
            dtp.FillColor = Color.White;
            dtp.Font = Font(10);
            dtp.Format = DateTimePickerFormat.Short;
        }

        // ===== Form =====
        public static void StyleForm(Form form)
        {
            form.BackColor = Background;
            form.Font = Font(10);
        }

        /// <summary>
        /// Sudut membulat di tiap form. Simpan hasil return-nya di field
        /// form supaya objeknya tidak di-GC (Guna2Elipse adalah Component).
        /// </summary>
        public static Guna2Elipse ApplyRoundedForm(Form form)
        {
            var elipse = new Guna2Elipse();
            elipse.TargetControl = form;
            elipse.BorderRadius = 12;
            return elipse;
        }

        /// <summary>
        /// Panel header biru solid (Dock = Top, tinggi 64) dengan judul +
        /// subjudul. Warna datar tanpa gradasi (keputusan gaya Fase 2:
        /// modern flat solid). Ditambahkan dari code-behind setelah
        /// InitializeComponent.
        /// </summary>
        public static Guna2Panel BuildHeader(string title, string subtitle)
        {
            var panel = new Guna2Panel();
            panel.Dock = DockStyle.Top;
            panel.Height = 64;
            panel.FillColor = Primary;
            panel.BorderRadius = 0;

            var lblTitle = new Label();
            lblTitle.Text = title;
            lblTitle.ForeColor = Color.White;
            // Label transparan di atas Guna2Panel mewarisi putih (Guna2Panel
            // melukis FillColor di OnPaint, bukan OnPaintBackground) — beri
            // BackColor opak yang sama dengan panel supaya teks terbaca.
            lblTitle.BackColor = Primary;
            lblTitle.Font = Font(16, FontStyle.Bold);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(20, 10);
            panel.Controls.Add(lblTitle);

            var lblSub = new Label();
            lblSub.Text = subtitle;
            lblSub.ForeColor = HeaderSub;
            lblSub.BackColor = Primary;
            lblSub.Font = Font(10);
            lblSub.AutoSize = true;
            lblSub.Location = new Point(20, 38);
            panel.Controls.Add(lblSub);

            return panel;
        }

        /// <summary>
        /// Kartu KPI putih dengan strip aksen 4px di atas.
        /// Label isi kartu diposisikan absolut di Designer (mulai y = 14).
        /// Kalau kartunya Guna2Panel, sudutnya ikut dibulatkan.
        /// </summary>
        public static void StyleCard(Panel card, Color accent)
        {
            card.BackColor = Card;
            Guna2Panel gunaCard = card as Guna2Panel;
            if (gunaCard != null)
            {
                gunaCard.FillColor = Card;
                gunaCard.BorderRadius = 12;
            }
            var strip = new Panel();
            strip.Dock = DockStyle.Top;
            strip.Height = 4;
            strip.BackColor = accent;
            card.Controls.Add(strip);
            strip.BringToFront();
        }
    }
}
