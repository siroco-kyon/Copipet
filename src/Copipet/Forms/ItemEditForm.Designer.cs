namespace Copipet.Forms
{
    partial class ItemEditForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        private void InitializeComponent()
        {
            this.labelTitle = new System.Windows.Forms.Label();
            this.textTitle = new System.Windows.Forms.TextBox();
            this.labelContent = new System.Windows.Forms.Label();
            this.textContent = new System.Windows.Forms.TextBox();
            this.buttonSave = new System.Windows.Forms.Button();
            this.buttonBack = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // labelTitle
            //
            this.labelTitle.AutoSize = true;
            this.labelTitle.Location = new System.Drawing.Point(12, 12);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(200, 17);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "タイトル（ボタンに表示する名前・30文字まで）";
            //
            // textTitle
            //
            this.textTitle.Location = new System.Drawing.Point(12, 34);
            this.textTitle.MaxLength = 30;
            this.textTitle.Name = "textTitle";
            this.textTitle.Size = new System.Drawing.Size(456, 25);
            this.textTitle.TabIndex = 1;
            //
            // labelContent
            //
            this.labelContent.AutoSize = true;
            this.labelContent.Location = new System.Drawing.Point(12, 70);
            this.labelContent.Name = "labelContent";
            this.labelContent.Size = new System.Drawing.Size(200, 17);
            this.labelContent.TabIndex = 2;
            this.labelContent.Text = "内容（クリックでコピーされる文章）";
            //
            // textContent
            //
            this.textContent.AcceptsReturn = true;
            this.textContent.Location = new System.Drawing.Point(12, 92);
            this.textContent.Multiline = true;
            this.textContent.Name = "textContent";
            this.textContent.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textContent.Size = new System.Drawing.Size(456, 250);
            this.textContent.TabIndex = 3;
            //
            // buttonSave
            //
            this.buttonSave.Location = new System.Drawing.Point(286, 354);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(88, 32);
            this.buttonSave.TabIndex = 4;
            this.buttonSave.Text = "保存";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.OnSaveClick);
            //
            // buttonBack
            //
            this.buttonBack.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonBack.Location = new System.Drawing.Point(380, 354);
            this.buttonBack.Name = "buttonBack";
            this.buttonBack.Size = new System.Drawing.Size(88, 32);
            this.buttonBack.TabIndex = 5;
            this.buttonBack.Text = "戻る";
            this.buttonBack.UseVisualStyleBackColor = true;
            //
            // ItemEditForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.buttonBack;
            this.ClientSize = new System.Drawing.Size(480, 398);
            this.Controls.Add(this.labelTitle);
            this.Controls.Add(this.textTitle);
            this.Controls.Add(this.labelContent);
            this.Controls.Add(this.textContent);
            this.Controls.Add(this.buttonSave);
            this.Controls.Add(this.buttonBack);
            this.Font = new System.Drawing.Font("Yu Gothic UI", 9.75F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ItemEditForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "コピー内容の編集";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.TextBox textTitle;
        private System.Windows.Forms.Label labelContent;
        private System.Windows.Forms.TextBox textContent;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Button buttonBack;
    }
}
