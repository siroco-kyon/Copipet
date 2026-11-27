using System;
using System.Windows.Forms;
using Copipet.Models;
using Copipet.Services;

namespace Copipet.Forms
{
    /// <summary>タブ名の変更画面。Enter または Ctrl+S で保存、Esc で戻る。</summary>
    public partial class TabNameForm : Form
    {
        public string TabName { get; private set; }

        /// <summary>デザイナー用。</summary>
        public TabNameForm()
        {
            InitializeComponent();
        }

        internal TabNameForm(string currentName, Palette palette)
            : this()
        {
            textName.MaxLength = AppData.MaxTabNameLength;
            textName.Text = currentName ?? string.Empty;
            textName.SelectAll();
            ThemeHelper.ApplyToDialog(this, palette, buttonSave);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            {
                Save();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            Save();
        }

        private void Save()
        {
            TabName = textName.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
