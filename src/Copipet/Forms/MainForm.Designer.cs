namespace Copipet.Forms
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            this.panelTabs = new System.Windows.Forms.Panel();
            this.panelItems = new System.Windows.Forms.Panel();
            this.labelStatus = new System.Windows.Forms.Label();
            this.buttonSettings = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // panelTabs
            // 
            this.panelTabs.Location = new System.Drawing.Point(12, 12);
            this.panelTabs.Name = "panelTabs";
            this.panelTabs.Size = new System.Drawing.Size(446, 64);
            this.panelTabs.TabIndex = 0;
            // 
            // panelItems
            // 
            this.panelItems.AutoScroll = true;
            this.panelItems.Location = new System.Drawing.Point(12, 88);
            this.panelItems.Name = "panelItems";
            this.panelItems.Size = new System.Drawing.Size(446, 244);
            this.panelItems.TabIndex = 1;
            // 
            // labelStatus
            // 
            this.labelStatus.AutoEllipsis = true;
            this.labelStatus.Location = new System.Drawing.Point(12, 342);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(406, 28);
            this.labelStatus.TabIndex = 2;
            this.labelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.labelStatus.UseMnemonic = false;
            // 
            // buttonSettings
            // 
            this.buttonSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonSettings.Font = new System.Drawing.Font("Segoe UI Symbol", 11F);
            this.buttonSettings.Location = new System.Drawing.Point(426, 342);
            this.buttonSettings.Name = "buttonSettings";
            this.buttonSettings.Size = new System.Drawing.Size(32, 28);
            this.buttonSettings.TabIndex = 3;
            this.buttonSettings.Text = "⚙";
            this.buttonSettings.UseVisualStyleBackColor = false;
            // 
            // toolTip
            // 
            this.toolTip.AutoPopDelay = 15000;
            this.toolTip.InitialDelay = 400;
            this.toolTip.OwnerDraw = true;
            this.toolTip.ReshowDelay = 100;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(470, 380);
            this.Controls.Add(this.panelTabs);
            this.Controls.Add(this.panelItems);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.buttonSettings);
            this.Font = new System.Drawing.Font("Yu Gothic UI", 9.75F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "コピペット";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelTabs;
        private System.Windows.Forms.Panel panelItems;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Button buttonSettings;
        private System.Windows.Forms.ToolTip toolTip;
    }
}
