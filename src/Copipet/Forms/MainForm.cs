using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Copipet.Models;
using Copipet.Services;

namespace Copipet.Forms
{
    /// <summary>トップ画面。タブ（最大8つ）と、タブごとのコピーボタン（0〜20件）を表示する。</summary>
    public partial class MainForm : Form
    {
        private const int StatusDisplayMs = 2000;
        private const int PreviewLength = 100;

        private readonly DataStore _store;
        private readonly AppData _data;
        private readonly string _startupNotice;
        private readonly List<Button> _itemButtons = new List<Button>();
        private Button _addItemButton;
        private ContextMenuStrip _itemMenu;
        private ToolStripMenuItem _menuItemEdit, _menuItemMovePrev, _menuItemMoveNext, _menuItemDelete;
        private int _menuItemIndex = -1;
        private readonly List<Button> _tabButtons = new List<Button>();
        private Button _addTabButton;
        private ContextMenuStrip _tabMenu;
        private ContextMenuStrip _settingsMenu;
        private ToolStripMenuItem _menuRename, _menuMoveLeft, _menuMoveRight, _menuDelete;
        private ToolStripMenuItem _menuThemeAuto, _menuThemeLight, _menuThemeDark, _menuTopMost;
        private ToolStripMenuItem _menuLayoutTwo, _menuLayoutOne;
        private LayoutSpec _layout = LayoutSpec.TwoColumns;
        private float _scale = 1f;
        private Timer _statusTimer;
        private Font _tabFont;
        private Font _tabFontBold;
        private Palette _palette;
        private int _currentTab;
        private int _menuTabIndex = -1;
        private int _savedLastTab;

        internal MainForm(DataStore store, AppData data, string startupNotice)
        {
            InitializeComponent();

            _store = store;
            _data = data;
            _startupNotice = startupNotice;
            _currentTab = data.LastTab;
            _savedLastTab = data.LastTab;

            _addItemButton = new Button
            {
                Text = "＋ 追加",
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                TabIndex = 1000,
            };
            _addItemButton.Click += (s, e) => AddItem();
            panelItems.Controls.Add(_addItemButton);
            panelItems.HandleCreated += (s, e) => ThemeHelper.ApplyControlTheme(panelItems, _palette.IsDark);

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (Exception)
            {
                // アイコンが取れなくても動作に影響はない
            }

            toolTip.Draw += OnToolTipDraw;
            toolTip.Popup += OnToolTipPopup;
            panelTabs.Layout += (s, e) => LayoutTabButtons();
            buttonSettings.Click += (s, e) => _settingsMenu.Show(buttonSettings, new Point(buttonSettings.Width, 0), ToolStripDropDownDirection.AboveLeft);

            _tabFont = new Font(Font.FontFamily, 9F, FontStyle.Regular);
            _tabFontBold = new Font(Font.FontFamily, 9F, FontStyle.Bold);
            components.Add(new FontHolder(_tabFont));
            components.Add(new FontHolder(_tabFontBold));

            BuildMenus();
            TopMost = _data.TopMost;
            _palette = ThemeHelper.Resolve(_data.Theme);
            _layout = LayoutSpec.For(_data.Layout);
            RebuildTabButtons();
            ApplyTheme();
            ShowTab(_currentTab);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // 自動スケーリングが済んだ後で、現在の拡大率を基に配置する
            _scale = MeasureScale();
            ApplyLayout(true);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ThemeHelper.ApplyTitleBar(this, _palette.IsDark);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_startupNotice != null)
            {
                MessageBox.Show(this, _startupNotice, "コピペット", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 最後に選んでいたタブを覚えておく（変わっていなければ書き込まない）
            if (_data.LastTab != _savedLastTab)
            {
                try
                {
                    _store.Save(_data);
                }
                catch (Exception)
                {
                    // 終了時はタブ位置が保存できなくても支障はない
                }
            }
            base.OnFormClosing(e);
        }

        // ------------------------------------------------------------------
        // タブ
        // ------------------------------------------------------------------

        private void RebuildTabButtons()
        {
            panelTabs.SuspendLayout();
            foreach (Button b in _tabButtons)
            {
                toolTip.SetToolTip(b, null);
                panelTabs.Controls.Remove(b);
                b.Dispose();
            }
            _tabButtons.Clear();

            for (int i = 0; i < _data.Tabs.Count; i++)
            {
                var button = new Button
                {
                    Tag = i,
                    Text = _data.Tabs[i].Name,
                    AutoEllipsis = true,
                    UseMnemonic = false,
                    FlatStyle = FlatStyle.Flat,
                    UseVisualStyleBackColor = false,
                    TabStop = false,
                };
                button.FlatAppearance.BorderSize = 0;
                toolTip.SetToolTip(button, _data.Tabs[i].Name);
                button.Click += OnTabButtonClick;
                button.MouseUp += OnTabButtonMouseUp;
                _tabButtons.Add(button);
                panelTabs.Controls.Add(button);
            }

            if (_addTabButton == null)
            {
                _addTabButton = new Button
                {
                    Text = "＋",
                    FlatStyle = FlatStyle.Flat,
                    UseVisualStyleBackColor = false,
                    TabStop = false,
                };
                _addTabButton.FlatAppearance.BorderSize = 1;
                _addTabButton.Click += (s, e) => AddTab();
                toolTip.SetToolTip(_addTabButton, "タブを追加（最大" + AppData.MaxTabs + "つ）");
                panelTabs.Controls.Add(_addTabButton);
            }
            _addTabButton.Visible = _data.Tabs.Count < AppData.MaxTabs;

            panelTabs.ResumeLayout(false);
            LayoutTabButtons();
            UpdateTabColors();
        }

        /// <summary>タブを4つずつ2段に並べる。panelTabs の大きさから計算するので高DPIでも崩れない。</summary>
        private void LayoutTabButtons()
        {
            if (_tabButtons.Count == 0) return;
            int columns = _layout.TabColumns;
            int rows = _layout.TabRows;
            int gap = (int)Math.Round(6 * _scale);
            int width = (panelTabs.ClientSize.Width - gap * (columns - 1)) / columns;
            int height = (panelTabs.ClientSize.Height - gap * (rows - 1)) / rows;

            for (int slot = 0; slot <= _tabButtons.Count && slot < columns * rows; slot++)
            {
                Button target = slot < _tabButtons.Count ? _tabButtons[slot] : _addTabButton;
                if (target == null) continue;
                int col = slot % columns;
                int row = slot / columns;
                target.SetBounds(col * (width + gap), row * (height + gap), width, height);
            }
        }

        private void UpdateTabColors()
        {
            Palette p = _palette;
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                Button b = _tabButtons[i];
                bool selected = i == _currentTab;
                Color back = selected ? p.TabSelectedBack : p.TabBack;
                b.BackColor = back;
                b.ForeColor = selected ? p.TabSelectedText : p.TabText;
                b.Font = selected ? _tabFontBold : _tabFont;
                Color hoverTarget = p.IsDark ? Color.White : Color.Black;
                b.FlatAppearance.MouseOverBackColor = selected ? back : Palette.Blend(back, hoverTarget, 0.12);
                b.FlatAppearance.MouseDownBackColor = Palette.Blend(back, hoverTarget, 0.2);
            }
            if (_addTabButton != null)
            {
                _addTabButton.BackColor = p.Back;
                _addTabButton.ForeColor = p.SubText;
                _addTabButton.FlatAppearance.BorderColor = p.Border;
                _addTabButton.FlatAppearance.MouseOverBackColor = p.ButtonHover;
                _addTabButton.FlatAppearance.MouseDownBackColor = p.ButtonPressed;
            }
        }

        private void ShowTab(int index)
        {
            if (index < 0) index = 0;
            if (index >= _data.Tabs.Count) index = _data.Tabs.Count - 1;
            if (index != _currentTab) panelItems.AutoScrollPosition = Point.Empty;
            _currentTab = index;
            _data.LastTab = index;
            UpdateTabColors();
            UpdateCopyButtons();
        }

        private void OnTabButtonClick(object sender, EventArgs e)
        {
            ShowTab((int)((Button)sender).Tag);
        }

        private void OnTabButtonMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            var button = (Button)sender;
            _menuTabIndex = (int)button.Tag;
            _tabMenu.Show(button, e.Location);
        }

        private void AddTab()
        {
            if (_data.Tabs.Count >= AppData.MaxTabs) return;

            var used = new HashSet<string>();
            foreach (TabData t in _data.Tabs) used.Add(t.Name);
            int n = _data.Tabs.Count;
            string name = AppData.DefaultTabName(n);
            while (used.Contains(name)) name = AppData.DefaultTabName(++n);

            _data.Tabs.Add(TabData.Create(name));
            RebuildTabButtons();
            ShowTab(_data.Tabs.Count - 1);
            SaveData();
        }

        private void RenameTab(int index)
        {
            TabData tab = _data.Tabs[index];
            using (var form = new TabNameForm(tab.Name, _palette))
            {
                if (form.ShowDialog(this) != DialogResult.OK) return;
                string name = form.TabName;
                tab.Name = name.Length == 0 ? AppData.DefaultTabName(index) : name;
            }
            _tabButtons[index].Text = tab.Name;
            toolTip.SetToolTip(_tabButtons[index], tab.Name);
            SaveData();
        }

        private void MoveTab(int index, int offset)
        {
            int target = index + offset;
            if (target < 0 || target >= _data.Tabs.Count) return;

            TabData tmp = _data.Tabs[index];
            _data.Tabs[index] = _data.Tabs[target];
            _data.Tabs[target] = tmp;

            // 選択中のタブを動かしたときは、選択も一緒に動かす
            if (_currentTab == index) _currentTab = target;
            else if (_currentTab == target) _currentTab = index;

            for (int i = 0; i < _tabButtons.Count; i++)
            {
                _tabButtons[i].Text = _data.Tabs[i].Name;
                toolTip.SetToolTip(_tabButtons[i], _data.Tabs[i].Name);
            }
            ShowTab(_currentTab);
            SaveData();
        }

        private void DeleteTab(int index)
        {
            if (_data.Tabs.Count <= 1) return;
            TabData tab = _data.Tabs[index];
            int registered = tab.RegisteredCount;

            string message = registered > 0
                ? "タブ「" + tab.Name + "」を削除します。\n登録済みの " + registered + " 件も消えます。よろしいですか？"
                : "タブ「" + tab.Name + "」を削除します。よろしいですか？";
            if (MessageBox.Show(this, message, "タブの削除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            _data.Tabs.RemoveAt(index);
            if (_currentTab > index || _currentTab >= _data.Tabs.Count) _currentTab--;
            if (_currentTab < 0) _currentTab = 0;

            RebuildTabButtons();
            ShowTab(_currentTab);
            SaveData();
        }

        // ------------------------------------------------------------------
        // コピーボタン（タブごとに0〜20件。ボタン欄だけが縦にスクロールする）
        // ------------------------------------------------------------------

        /// <summary>今のタブの件数に合わせてボタンを用意し、表示と配置を更新する。</summary>
        private void UpdateCopyButtons()
        {
            TabData tab = _data.Tabs[_currentTab];
            panelItems.SuspendLayout();

            // ボタンは必要になった数だけ作り、以後は使い回す
            while (_itemButtons.Count < tab.Items.Count)
            {
                var button = new Button
                {
                    Tag = _itemButtons.Count,
                    TabIndex = _itemButtons.Count,
                    AutoEllipsis = true,
                    UseMnemonic = false,
                    FlatStyle = FlatStyle.Flat,
                    UseVisualStyleBackColor = false,
                };
                button.Click += OnCopyButtonClick;
                button.MouseUp += OnCopyButtonMouseUp;
                StyleItemButton(button);
                _itemButtons.Add(button);
                panelItems.Controls.Add(button);
            }

            for (int i = 0; i < _itemButtons.Count; i++)
            {
                Button b = _itemButtons[i];
                if (i >= tab.Items.Count)
                {
                    b.Visible = false;
                    toolTip.SetToolTip(b, null);
                    continue;
                }

                ItemData item = tab.Items[i];
                if (item.IsEmpty)
                {
                    b.Text = item.Title.Length > 0 ? item.Title : "（未登録）";
                    b.ForeColor = _palette.EmptyText;
                    toolTip.SetToolTip(b, "クリックして登録");
                }
                else
                {
                    b.Text = item.Title.Length > 0 ? item.Title : "（無題）";
                    b.ForeColor = _palette.Text;
                    toolTip.SetToolTip(b, MakePreview(item.Content));
                }
                b.Visible = true;
            }

            int remaining = AppData.MaxItemsPerTab - tab.Items.Count;
            _addItemButton.Visible = remaining > 0;
            _addItemButton.Text = tab.Items.Count == 0 ? "＋ 追加（最初の1件を登録）" : "＋ 追加";
            toolTip.SetToolTip(_addItemButton, "追加（あと" + remaining + "件）");

            panelItems.ResumeLayout(false);
            LayoutItemButtons();
        }

        /// <summary>ボタンを並べる。2列は左→右、上→下の順。入りきらないときは縦スクロールバーの分だけ幅を詰める。</summary>
        private void LayoutItemButtons()
        {
            TabData tab = _data.Tabs[_currentTab];
            LayoutSpec l = _layout;
            int count = tab.Items.Count + (_addItemButton.Visible ? 1 : 0);
            int columns = l.ButtonColumns;
            int rows = (count + columns - 1) / columns;
            int gap = S(l.Gap);
            int height = S(l.ButtonHeight);
            int contentHeight = rows * (height + gap) - gap;
            bool needScroll = contentHeight > panelItems.Height;
            int available = panelItems.Width - (needScroll ? SystemInformation.VerticalScrollBarWidth + S(2) : 0);
            int width = (available - gap * (columns - 1)) / columns;
            int offsetY = panelItems.AutoScrollPosition.Y;

            panelItems.SuspendLayout();
            for (int i = 0; i < count; i++)
            {
                Button b = i < tab.Items.Count ? _itemButtons[i] : _addItemButton;
                int col = i % columns;
                int row = i / columns;
                b.SetBounds(col * (width + gap), offsetY + row * (height + gap), width, height);
            }
            panelItems.ResumeLayout(true);
        }

        private void StyleItemButton(Button b)
        {
            b.BackColor = _palette.ButtonBack;
            b.FlatAppearance.BorderColor = _palette.Border;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.MouseOverBackColor = _palette.ButtonHover;
            b.FlatAppearance.MouseDownBackColor = _palette.ButtonPressed;
        }

        private static string MakePreview(string content)
        {
            string text = content.Replace("\r\n", "\n").Replace('\r', '\n');
            if (text.Length > PreviewLength) text = text.Substring(0, PreviewLength) + "…";
            return text;
        }

        private void OnCopyButtonClick(object sender, EventArgs e)
        {
            int index = (int)((Button)sender).Tag;
            ItemData item = _data.Tabs[_currentTab].Items[index];

            if (item.IsEmpty)
            {
                OpenItemEditor(index);
                return;
            }

            string label = item.Title.Length > 0 ? item.Title : "（無題）";
            if (ClipboardHelper.TrySetText(item.Content))
            {
                ShowStatus("コピーしました：" + label);
            }
            else
            {
                ShowStatus("コピーできませんでした（他のアプリがクリップボードを使用中）");
            }
        }

        private void OnCopyButtonMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            var button = (Button)sender;
            _menuItemIndex = (int)button.Tag;
            _itemMenu.Show(button, e.Location);
        }

        /// <summary>編集画面を開く。保存したら true。</summary>
        private bool OpenItemEditor(int index)
        {
            TabData tab = _data.Tabs[_currentTab];
            ItemData item = tab.Items[index];
            string caption = "コピー内容の編集 - " + tab.Name + " No." + (index + 1);

            using (var form = new ItemEditForm(caption, item.Title, item.Content, _palette))
            {
                if (form.ShowDialog(this) != DialogResult.OK) return false;
                item.Title = form.ItemTitle;
                item.Content = form.ItemContent;
            }
            UpdateCopyButtons();
            SaveData();
            return true;
        }

        /// <summary>末尾に1件追加して編集画面を開く。戻るを押したか、空のまま保存したら追加を取り消す。</summary>
        private void AddItem()
        {
            TabData tab = _data.Tabs[_currentTab];
            if (tab.Items.Count >= AppData.MaxItemsPerTab) return;

            tab.Items.Add(new ItemData());
            int index = tab.Items.Count - 1;
            UpdateCopyButtons();
            panelItems.ScrollControlIntoView(_itemButtons[index]);

            bool saved = OpenItemEditor(index);
            if (!saved || tab.Items[index].IsBlank)
            {
                tab.Items.RemoveAt(index);
                UpdateCopyButtons();
                if (saved) SaveData();
            }
        }

        private void MoveItem(int index, int offset)
        {
            List<ItemData> items = _data.Tabs[_currentTab].Items;
            int target = index + offset;
            if (index < 0 || target < 0 || target >= items.Count) return;

            ItemData tmp = items[index];
            items[index] = items[target];
            items[target] = tmp;
            UpdateCopyButtons();
            SaveData();
        }

        private void DeleteItem(int index)
        {
            List<ItemData> items = _data.Tabs[_currentTab].Items;
            if (index < 0 || index >= items.Count) return;
            ItemData item = items[index];

            if (!item.IsBlank)
            {
                string name = item.Title.Length > 0 ? item.Title : "（無題）";
                if (MessageBox.Show(this, "「" + name + "」を削除します。よろしいですか？", "削除",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                {
                    return;
                }
            }

            items.RemoveAt(index);
            UpdateCopyButtons();
            SaveData();
        }

        // ------------------------------------------------------------------
        // メニュー・テーマ・設定
        // ------------------------------------------------------------------

        private void BuildMenus()
        {
            _menuRename = new ToolStripMenuItem("名前を変更", null, (s, e) => RenameTab(_menuTabIndex));
            _menuMoveLeft = new ToolStripMenuItem("左へ移動", null, (s, e) => MoveTab(_menuTabIndex, -1));
            _menuMoveRight = new ToolStripMenuItem("右へ移動", null, (s, e) => MoveTab(_menuTabIndex, +1));
            _menuDelete = new ToolStripMenuItem("削除", null, (s, e) => DeleteTab(_menuTabIndex));
            _tabMenu = new ContextMenuStrip(components);
            _tabMenu.Items.AddRange(new ToolStripItem[] { _menuRename, _menuMoveLeft, _menuMoveRight, new ToolStripSeparator(), _menuDelete });
            _tabMenu.Opening += (s, e) =>
            {
                _menuMoveLeft.Enabled = _menuTabIndex > 0;
                _menuMoveRight.Enabled = _menuTabIndex < _data.Tabs.Count - 1;
                _menuDelete.Enabled = _data.Tabs.Count > 1;
            };

            _menuItemEdit = new ToolStripMenuItem("編集", null, (s, e) => OpenItemEditor(_menuItemIndex));
            _menuItemEdit.Font = new Font(_menuItemEdit.Font, FontStyle.Bold);
            _menuItemMovePrev = new ToolStripMenuItem("前へ移動", null, (s, e) => MoveItem(_menuItemIndex, -1));
            _menuItemMoveNext = new ToolStripMenuItem("後ろへ移動", null, (s, e) => MoveItem(_menuItemIndex, +1));
            _menuItemDelete = new ToolStripMenuItem("削除", null, (s, e) => DeleteItem(_menuItemIndex));
            _itemMenu = new ContextMenuStrip(components);
            _itemMenu.Items.AddRange(new ToolStripItem[] { _menuItemEdit, _menuItemMovePrev, _menuItemMoveNext, new ToolStripSeparator(), _menuItemDelete });
            _itemMenu.Opening += (s, e) =>
            {
                bool oneColumn = _layout.ButtonColumns == 1;
                _menuItemMovePrev.Text = oneColumn ? "上へ移動" : "前へ移動";
                _menuItemMoveNext.Text = oneColumn ? "下へ移動" : "後ろへ移動";
                _menuItemMovePrev.Enabled = _menuItemIndex > 0;
                _menuItemMoveNext.Enabled = _menuItemIndex < _data.Tabs[_currentTab].Items.Count - 1;
            };

            _menuThemeAuto = new ToolStripMenuItem("Windowsの設定に合わせる", null, (s, e) => ChangeTheme(AppData.ThemeAuto));
            _menuThemeLight = new ToolStripMenuItem("ライト", null, (s, e) => ChangeTheme(AppData.ThemeLight));
            _menuThemeDark = new ToolStripMenuItem("ダーク", null, (s, e) => ChangeTheme(AppData.ThemeDark));
            var themeMenu = new ToolStripMenuItem("テーマ");
            themeMenu.DropDownItems.AddRange(new ToolStripItem[] { _menuThemeAuto, _menuThemeLight, _menuThemeDark });
            _menuLayoutTwo = new ToolStripMenuItem("2列（横長）", null, (s, e) => ChangeLayout(AppData.LayoutTwoColumns));
            _menuLayoutOne = new ToolStripMenuItem("1列（縦長・幅が狭い）", null, (s, e) => ChangeLayout(AppData.LayoutOneColumn));
            var layoutMenu = new ToolStripMenuItem("レイアウト");
            layoutMenu.DropDownItems.AddRange(new ToolStripItem[] { _menuLayoutTwo, _menuLayoutOne });
            _menuTopMost = new ToolStripMenuItem("常に最前面に表示", null, (s, e) => ToggleTopMost());
            var openFolder = new ToolStripMenuItem("データフォルダを開く", null, (s, e) => OpenDataFolder());

            _settingsMenu = new ContextMenuStrip(components);
            _settingsMenu.Items.AddRange(new ToolStripItem[] { themeMenu, layoutMenu, _menuTopMost, new ToolStripSeparator(), openFolder });
            _settingsMenu.Opening += (s, e) =>
            {
                _menuThemeAuto.Checked = _data.Theme == AppData.ThemeAuto;
                _menuThemeLight.Checked = _data.Theme == AppData.ThemeLight;
                _menuThemeDark.Checked = _data.Theme == AppData.ThemeDark;
                _menuTopMost.Checked = _data.TopMost;
                _menuLayoutTwo.Checked = _data.Layout == AppData.LayoutTwoColumns;
                _menuLayoutOne.Checked = _data.Layout == AppData.LayoutOneColumn;
            };
        }

        private void ChangeTheme(string theme)
        {
            _data.Theme = theme;
            _palette = ThemeHelper.Resolve(theme);
            ApplyTheme();
            UpdateTabColors();
            UpdateCopyButtons();
            SaveData();
        }

        private void ChangeLayout(string layout)
        {
            if (_data.Layout == layout) return;
            _data.Layout = layout;
            _layout = LayoutSpec.For(layout);
            ApplyLayout(false);
            SaveData();
        }

        /// <summary>
        /// レイアウト（2列／1列）に合わせて部品を並べ直す。値は 100% 表示時の px で、拡大率を掛けて使う。
        /// </summary>
        private void ApplyLayout(bool center)
        {
            LayoutSpec l = _layout;
            SuspendLayout();

            panelTabs.Bounds = Scaled(l.Margin, l.Margin, l.ContentWidth, l.TabAreaHeight);

            int top = l.Margin + l.TabAreaHeight + l.Margin;
            int viewport = l.VisibleRows * (l.ButtonHeight + l.Gap) - l.Gap;
            panelItems.Bounds = Scaled(l.Margin, top, l.ContentWidth, viewport);

            int statusTop = top + viewport + 10;
            const int settingsWidth = 32, barHeight = 28;
            labelStatus.Bounds = Scaled(l.Margin, statusTop, l.ContentWidth - settingsWidth - 8, barHeight);
            buttonSettings.Bounds = Scaled(l.Margin + l.ContentWidth - settingsWidth, statusTop, settingsWidth, barHeight);

            ClientSize = new Size(S(l.Margin * 2 + l.ContentWidth), S(statusTop + barHeight + 10));
            ResumeLayout(true);
            LayoutTabButtons();
            LayoutItemButtons();

            if (center) CenterToScreen();
            else KeepOnScreen();
        }

        private void KeepOnScreen()
        {
            Rectangle area = Screen.FromControl(this).WorkingArea;
            int x = Math.Max(area.Left, Math.Min(Left, area.Right - Width));
            int y = Math.Max(area.Top, Math.Min(Top, area.Bottom - Height));
            Location = new Point(x, y);
        }

        private int S(int value)
        {
            return (int)Math.Round(value * _scale);
        }

        private Rectangle Scaled(int x, int y, int width, int height)
        {
            return new Rectangle(S(x), S(y), S(width), S(height));
        }

        /// <summary>画面の拡大率（100%=1.0）。</summary>
        private float MeasureScale()
        {
            try
            {
                float dpi = CurrentAutoScaleDimensions.Width;
                if (dpi > 0) return Math.Max(1f, dpi / 96f);
            }
            catch (Exception)
            {
                // 取得できない環境では下の方法で求める
            }
            using (Graphics g = CreateGraphics())
            {
                return Math.Max(1f, g.DpiX / 96f);
            }
        }

        private void ToggleTopMost()
        {
            _data.TopMost = !_data.TopMost;
            TopMost = _data.TopMost;
            SaveData();
        }

        private void OpenDataFolder()
        {
            try
            {
                Directory.CreateDirectory(_store.DirectoryPath);
                Process.Start("explorer.exe", "\"" + _store.DirectoryPath + "\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "フォルダを開けませんでした。\n\n" + ex.Message, "コピペット", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyTheme()
        {
            Palette p = _palette;
            BackColor = p.Back;
            ForeColor = p.Text;
            panelTabs.BackColor = p.Back;
            labelStatus.ForeColor = p.SubText;

            panelItems.BackColor = p.Back;
            foreach (Button b in _itemButtons) StyleItemButton(b);
            StyleItemButton(_addItemButton);
            _addItemButton.BackColor = p.Back;
            _addItemButton.ForeColor = p.SubText;
            ThemeHelper.ApplyControlTheme(panelItems, p.IsDark);

            buttonSettings.BackColor = p.Back;
            buttonSettings.ForeColor = p.SubText;
            buttonSettings.FlatAppearance.BorderSize = 0;
            buttonSettings.FlatAppearance.MouseOverBackColor = p.ButtonHover;
            buttonSettings.FlatAppearance.MouseDownBackColor = p.ButtonPressed;

            var renderer = new ThemedMenuRenderer(p);
            _tabMenu.Renderer = renderer;
            _itemMenu.Renderer = renderer;
            _settingsMenu.Renderer = renderer;
            foreach (ToolStripItem item in _settingsMenu.Items)
            {
                var menuItem = item as ToolStripMenuItem;
                if (menuItem != null && menuItem.HasDropDownItems) menuItem.DropDown.Renderer = renderer;
            }

            ThemeHelper.ApplyTitleBar(this, p.IsDark);
            Invalidate(true);
        }

        private void ShowStatus(string message)
        {
            labelStatus.Text = message;
            if (_statusTimer == null)
            {
                _statusTimer = new Timer(components) { Interval = StatusDisplayMs };
                _statusTimer.Tick += (s, e) =>
                {
                    _statusTimer.Stop();
                    labelStatus.Text = string.Empty;
                };
            }
            _statusTimer.Stop();
            _statusTimer.Start();
        }

        private void SaveData()
        {
            try
            {
                _store.Save(_data);
                _savedLastTab = _data.LastTab;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "保存できませんでした。\n\n" + _store.FilePath + "\n\n" + ex.Message,
                    "コピペット", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ------------------------------------------------------------------
        // ツールチップ（ダークでも読めるように自前で描く）
        // ------------------------------------------------------------------

        private const TextFormatFlags ToolTipFlags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.Left | TextFormatFlags.Top;

        private int ToolTipPadding
        {
            get { return S(6); }
        }

        private void OnToolTipPopup(object sender, PopupEventArgs e)
        {
            string text = toolTip.GetToolTip(e.AssociatedControl);
            int maxWidth = S(360);
            Size size = TextRenderer.MeasureText(text, Font, new Size(maxWidth, 0), ToolTipFlags);
            e.ToolTipSize = new Size(size.Width + ToolTipPadding * 2, size.Height + ToolTipPadding * 2);
        }

        private void OnToolTipDraw(object sender, DrawToolTipEventArgs e)
        {
            using (var back = new SolidBrush(_palette.ToolTipBack))
            using (var border = new Pen(_palette.Border))
            {
                e.Graphics.FillRectangle(back, e.Bounds);
                e.Graphics.DrawRectangle(border, new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1));
            }
            Rectangle textBounds = Rectangle.Inflate(e.Bounds, -ToolTipPadding, -ToolTipPadding);
            TextRenderer.DrawText(e.Graphics, e.ToolTipText, Font, textBounds, _palette.Text, ToolTipFlags);
        }
    }

    /// <summary>トップ画面の並べ方（値は 100% 表示時の px）。</summary>
    internal sealed class LayoutSpec
    {
        public int Margin { get; private set; }
        public int Gap { get; private set; }
        public int ContentWidth { get; private set; }
        public int TabColumns { get; private set; }
        public int TabRows { get; private set; }
        public int TabAreaHeight { get; private set; }
        public int ButtonColumns { get; private set; }
        public int ButtonHeight { get; private set; }

        /// <summary>スクロールせずに見える行数。</summary>
        public int VisibleRows { get; private set; }

        /// <summary>2列（横長）：タブ4つ×2段、ボタン2列（5段ぶん表示）。</summary>
        public static readonly LayoutSpec TwoColumns = new LayoutSpec
        {
            Margin = 12, Gap = 6, ContentWidth = 446,
            TabColumns = 4, TabRows = 2, TabAreaHeight = 64,
            ButtonColumns = 2, ButtonHeight = 44, VisibleRows = 5,
        };

        /// <summary>1列（縦長）：タブ2つ×4段、ボタン1列（10段ぶん表示）。</summary>
        public static readonly LayoutSpec OneColumn = new LayoutSpec
        {
            Margin = 12, Gap = 6, ContentWidth = 248,
            TabColumns = 2, TabRows = 4, TabAreaHeight = 134,
            ButtonColumns = 1, ButtonHeight = 36, VisibleRows = 10,
        };

        public static LayoutSpec For(string layout)
        {
            return layout == AppData.LayoutOneColumn ? OneColumn : TwoColumns;
        }
    }

    /// <summary>フォームの破棄時にフォントも破棄されるようにするための入れ物。</summary>
    internal sealed class FontHolder : System.ComponentModel.Component
    {
        private readonly Font _font;

        public FontHolder(Font font)
        {
            _font = font;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _font.Dispose();
            base.Dispose(disposing);
        }
    }
}
