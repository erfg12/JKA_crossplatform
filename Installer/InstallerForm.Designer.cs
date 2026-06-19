namespace Installer
{
    partial class InstallerForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InstallerForm));
            InstallBtn = new Button();
            OutputBox = new RichTextBox();
            InstallPathBox = new TextBox();
            BrowseBtn = new Button();
            InstallerPathGroupBox = new GroupBox();
            InstallationFolderBrowserDialog = new FolderBrowserDialog();
            InstallerPathGroupBox.SuspendLayout();
            SuspendLayout();
            // 
            // InstallBtn
            // 
            InstallBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            InstallBtn.Cursor = Cursors.Hand;
            InstallBtn.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            InstallBtn.ForeColor = Color.DarkSlateGray;
            InstallBtn.Location = new Point(628, 369);
            InstallBtn.Name = "InstallBtn";
            InstallBtn.Size = new Size(95, 38);
            InstallBtn.TabIndex = 0;
            InstallBtn.Text = "INSTALL";
            InstallBtn.UseVisualStyleBackColor = true;
            InstallBtn.Click += InstallBtn_Click;
            // 
            // OutputBox
            // 
            OutputBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            OutputBox.Location = new Point(12, 12);
            OutputBox.Name = "OutputBox";
            OutputBox.ReadOnly = true;
            OutputBox.Size = new Size(711, 342);
            OutputBox.TabIndex = 1;
            OutputBox.Text = "";
            // 
            // InstallPathBox
            // 
            InstallPathBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            InstallPathBox.Location = new Point(6, 18);
            InstallPathBox.Name = "InstallPathBox";
            InstallPathBox.Size = new Size(513, 23);
            InstallPathBox.TabIndex = 2;
            // 
            // BrowseBtn
            // 
            BrowseBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            BrowseBtn.Cursor = Cursors.Hand;
            BrowseBtn.Location = new Point(525, 18);
            BrowseBtn.Name = "BrowseBtn";
            BrowseBtn.Size = new Size(63, 23);
            BrowseBtn.TabIndex = 3;
            BrowseBtn.Text = "Browse";
            BrowseBtn.UseVisualStyleBackColor = true;
            BrowseBtn.Click += BrowseBtn_Click;
            // 
            // InstallerPathGroupBox
            // 
            InstallerPathGroupBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            InstallerPathGroupBox.Controls.Add(InstallPathBox);
            InstallerPathGroupBox.Controls.Add(BrowseBtn);
            InstallerPathGroupBox.Location = new Point(12, 360);
            InstallerPathGroupBox.Name = "InstallerPathGroupBox";
            InstallerPathGroupBox.Size = new Size(594, 47);
            InstallerPathGroupBox.TabIndex = 4;
            InstallerPathGroupBox.TabStop = false;
            InstallerPathGroupBox.Text = "Install Path";
            // 
            // InstallerForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(735, 415);
            Controls.Add(InstallerPathGroupBox);
            Controls.Add(OutputBox);
            Controls.Add(InstallBtn);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "InstallerForm";
            ShowIcon = false;
            Text = "JKACrossplatform Installer";
            Shown += InstallerForm_Shown;
            InstallerPathGroupBox.ResumeLayout(false);
            InstallerPathGroupBox.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Button InstallBtn;
        private RichTextBox OutputBox;
        private TextBox InstallPathBox;
        private Button BrowseBtn;
        private GroupBox InstallerPathGroupBox;
        private FolderBrowserDialog InstallationFolderBrowserDialog;
    }
}
