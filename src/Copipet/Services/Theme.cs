using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Copipet.Services
{
    /// <summary>ライト／ダークの配色。</summary>
    internal sealed class Palette
    {
        public bool IsDark { get; private set; }
        public Color Back { get; private set; }
        public Color ButtonBack { get; private set; }
        public Color ButtonHover { get; private set; }
        public Color ButtonPressed { get; private set; }
        public Color Border { get; private set; }
        public Color Text { get; private set; }
        public Color SubText { get; private set; }
        public Color EmptyText { get; private set; }
        public Color InputBack { get; private set; }
        public Color TabSelectedBack { get; private set; }
        public Color TabSelectedText { get; private set; }
        public Color TabBack { get; private set; }
        public Color TabText { get; private set; }
        public Color Accent { get; private set; }
        public Color AccentText { get; private set; }
        public Color ToolTipBack { get; private set; }
        public Color MenuHover { get; private set; }

        public static readonly Palette Light = new Palette
        {
            IsDark = false,
            Back = ColorTranslator.FromHtml("#F5F5F5"),
            ButtonBack = Color.White,
            ButtonHover = ColorTranslator.FromHtml("#E5F3FF"),
            ButtonPressed = ColorTranslator.FromHtml("#CCE8FF"),
            Border = ColorTranslator.FromHtml("#CCCCCC"),
            Text = ColorTranslator.FromHtml("#1E1E1E"),
            SubText = ColorTranslator.FromHtml("#5A5A5A"),
            EmptyText = ColorTranslator.FromHtml("#9A9A9A"),
            InputBack = Color.White,
            TabSelectedBack = Color.DeepSkyBlue,
            TabSelectedText = Color.Black,
            TabBack = Color.DarkGray,
            TabText = Color.Black,
            Accent = Color.DeepSkyBlue,
            AccentText = Color.Black,
            ToolTipBack = Color.White,
            MenuHover = ColorTranslator.FromHtml("#E5F3FF"),
        };

        public static readonly Palette Dark = new Palette
        {
            IsDark = true,
            Back = ColorTranslator.FromHtml("#1E1E1E"),
            ButtonBack = ColorTranslator.FromHtml("#2D2D30"),
            ButtonHover = ColorTranslator.FromHtml("#3E3E42"),
            ButtonPressed = ColorTranslator.FromHtml("#505056"),
            Border = ColorTranslator.FromHtml("#3F3F46"),
            Text = ColorTranslator.FromHtml("#F0F0F0"),
            SubText = ColorTranslator.FromHtml("#B4B4B4"),
            EmptyText = ColorTranslator.FromHtml("#7A7A7A"),
            InputBack = ColorTranslator.FromHtml("#252526"),
            TabSelectedBack = ColorTranslator.FromHtml("#1E90FF"),
            TabSelectedText = Color.White,
            TabBack = ColorTranslator.FromHtml("#3C3C3C"),
            TabText = ColorTranslator.FromHtml("#DDDDDD"),
            Accent = ColorTranslator.FromHtml("#1E90FF"),
            AccentText = Color.White,
            ToolTipBack = ColorTranslator.FromHtml("#2D2D30"),
            MenuHover = ColorTranslator.FromHtml("#3E3E42"),
        };

        /// <summary>2色を混ぜる（amount=0 で a、1 で b）。</summary>
        public static Color Blend(Color a, Color b, double amount)
        {
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * amount),
                (int)(a.G + (b.G - a.G) * amount),
                (int)(a.B + (b.B - a.B) * amount));
        }
    }

    internal static class ThemeHelper
    {
        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaUseImmersiveDarkModeOld = 19; // Windows 10 1809〜1909
        private const uint SwpNoSize = 0x0001, SwpNoMove = 0x0002, SwpNoZOrder = 0x0004, SwpNoActivate = 0x0010, SwpFrameChanged = 0x0020;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string subAppName, string subIdList);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        /// <summary>Windows の「アプリモード」がダークかどうか（読めない場合はライト扱い）。</summary>
        public static bool IsWindowsDark()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    object value = key == null ? null : key.GetValue("AppsUseLightTheme");
                    return value is int && (int)value == 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static Palette Resolve(string themeSetting)
        {
            if (themeSetting == Models.AppData.ThemeDark) return Palette.Dark;
            if (themeSetting == Models.AppData.ThemeLight) return Palette.Light;
            return IsWindowsDark() ? Palette.Dark : Palette.Light;
        }

        /// <summary>タイトルバーをダーク／ライトにする（古い Windows では何も起きない）。</summary>
        public static void ApplyTitleBar(Form form, bool dark)
        {
            if (!form.IsHandleCreated) return;
            try
            {
                int value = dark ? 1 : 0;
                if (DwmSetWindowAttribute(form.Handle, DwmwaUseImmersiveDarkMode, ref value, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(form.Handle, DwmwaUseImmersiveDarkModeOld, ref value, sizeof(int));
                }
                // 表示中に切り替えた場合もすぐ描き直されるようにする
                SetWindowPos(form.Handle, IntPtr.Zero, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }

        /// <summary>スクロールバーなどをダーク／ライトにする。</summary>
        public static void ApplyControlTheme(Control control, bool dark)
        {
            if (!control.IsHandleCreated) return;
            try
            {
                SetWindowTheme(control.Handle, dark ? "DarkMode_Explorer" : "Explorer", null);
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }

        /// <summary>普通のボタン（保存・戻るなど）の見た目。</summary>
        public static void StyleButton(Button button, Palette p, bool accent)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            Color back = accent ? p.Accent : p.ButtonBack;
            button.BackColor = back;
            button.ForeColor = accent ? p.AccentText : p.Text;
            button.FlatAppearance.BorderColor = accent ? p.Accent : p.Border;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = accent ? Palette.Blend(back, p.IsDark ? Color.White : Color.Black, 0.12) : p.ButtonHover;
            button.FlatAppearance.MouseDownBackColor = accent ? Palette.Blend(back, p.IsDark ? Color.White : Color.Black, 0.22) : p.ButtonPressed;
        }

        /// <summary>編集画面などのダイアログ全体に配色を当てる。</summary>
        public static void ApplyToDialog(Form form, Palette p, Button accentButton)
        {
            form.BackColor = p.Back;
            form.ForeColor = p.Text;
            foreach (Control c in form.Controls)
            {
                var textBox = c as TextBox;
                if (textBox != null)
                {
                    textBox.BackColor = p.InputBack;
                    textBox.ForeColor = p.Text;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    if (textBox.IsHandleCreated) ApplyControlTheme(textBox, p.IsDark);
                    else textBox.HandleCreated += (s, e) => ApplyControlTheme((Control)s, p.IsDark);
                    continue;
                }
                var button = c as Button;
                if (button != null)
                {
                    StyleButton(button, p, button == accentButton);
                    continue;
                }
                var label = c as Label;
                if (label != null)
                {
                    label.ForeColor = p.SubText;
                }
            }
            if (form.IsHandleCreated) ApplyTitleBar(form, p.IsDark);
            else form.HandleCreated += (s, e) => ApplyTitleBar((Form)s, p.IsDark);
        }
    }

    /// <summary>右クリックメニュー・⚙メニューをテーマに合わせて描くレンダラー。</summary>
    internal sealed class ThemedMenuRenderer : ToolStripProfessionalRenderer
    {
        private readonly Palette _p;

        public ThemedMenuRenderer(Palette p) : base(new ThemedColorTable(p))
        {
            _p = p;
            RoundedEdges = false;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? _p.Text : _p.EmptyText;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = _p.Text;
            base.OnRenderArrow(e);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            // 既定のチェック画像は黒一色でダークだと見えないため、文字で描く
            Rectangle r = e.ImageRectangle;
            if (r.Width <= 0) r = new Rectangle(e.Item.ContentRectangle.X + 4, e.Item.ContentRectangle.Y, 16, e.Item.ContentRectangle.Height);
            TextRenderer.DrawText(e.Graphics, "✓", e.Item.Font, r, _p.Text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        private sealed class ThemedColorTable : ProfessionalColorTable
        {
            private readonly Palette _p;

            public ThemedColorTable(Palette p)
            {
                _p = p;
                UseSystemColors = false;
            }

            public override Color ToolStripDropDownBackground { get { return _p.ButtonBack; } }
            public override Color ImageMarginGradientBegin { get { return _p.ButtonBack; } }
            public override Color ImageMarginGradientMiddle { get { return _p.ButtonBack; } }
            public override Color ImageMarginGradientEnd { get { return _p.ButtonBack; } }
            public override Color MenuBorder { get { return _p.Border; } }
            public override Color MenuItemBorder { get { return _p.MenuHover; } }
            public override Color MenuItemSelected { get { return _p.MenuHover; } }
            public override Color MenuItemSelectedGradientBegin { get { return _p.MenuHover; } }
            public override Color MenuItemSelectedGradientEnd { get { return _p.MenuHover; } }
            public override Color MenuItemPressedGradientBegin { get { return _p.MenuHover; } }
            public override Color MenuItemPressedGradientEnd { get { return _p.MenuHover; } }
            public override Color SeparatorDark { get { return _p.Border; } }
            public override Color SeparatorLight { get { return _p.Border; } }
            public override Color CheckBackground { get { return _p.ButtonBack; } }
            public override Color CheckSelectedBackground { get { return _p.MenuHover; } }
            public override Color CheckPressedBackground { get { return _p.MenuHover; } }
        }
    }
}
