using System;
using System.Windows.Forms;
using Copipet.Models;
using Copipet.Services;

namespace Copipet.Forms
{
    /// <summary>
    /// コピー内容の編集画面。保存で DialogResult.OK、戻る（Esc）で破棄して閉じる。
    /// タイトルと内容を両方空にして保存すると「未登録」に戻る。
    /// </summary>
    public partial class ItemEditForm : Form
    {
        public string ItemTitle { get; private set; }
        public string ItemContent { get; private set; }

        /// <summary>デザイナー用。</summary>
        public ItemEditForm()
        {
            InitializeComponent();
        }

        internal ItemEditForm(string caption, string title, string content, Palette palette)
            : this()
        {
            Text = caption;
            textTitle.MaxLength = AppData.MaxTitleLength;
            textTitle.Text = title ?? string.Empty;
            // 表示用に改行を CRLF に揃える（TextBox は LF だけだと改行されないため）
            textContent.Text = (content ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\r\n");
            ThemeHelper.ApplyToDialog(this, palette, buttonSave);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // 未登録ならタイトルから、登録済みなら内容から入力を始める
            if (textTitle.Text.Length == 0) textTitle.Focus();
            else
            {
                textContent.Focus();
                textContent.SelectionStart = textContent.TextLength;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            {
                Save();
                return true;
            }
            if (keyData == (Keys.Control | Keys.A) && ActiveControl is TextBox)
            {
                ((TextBox)ActiveControl).SelectAll();
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
            ItemTitle = textTitle.Text.Trim();
            ItemContent = textContent.Text;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
