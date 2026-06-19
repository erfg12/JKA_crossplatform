namespace Uninstaller
{
    partial class UninstallerForm
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
            OutputRichTextBox = new RichTextBox();
            UninstallBtn = new Button();
            DiscordBtn = new Button();
            SuspendLayout();
            // 
            // OutputRichTextBox
            // 
            OutputRichTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            OutputRichTextBox.Location = new Point(12, 12);
            OutputRichTextBox.Name = "OutputRichTextBox";
            OutputRichTextBox.ReadOnly = true;
            OutputRichTextBox.Size = new Size(668, 314);
            OutputRichTextBox.TabIndex = 0;
            OutputRichTextBox.Text = "";
            // 
            // UninstallBtn
            // 
            UninstallBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            UninstallBtn.Cursor = Cursors.Hand;
            UninstallBtn.Location = new Point(590, 334);
            UninstallBtn.Name = "UninstallBtn";
            UninstallBtn.Size = new Size(90, 23);
            UninstallBtn.TabIndex = 1;
            UninstallBtn.Text = "Uninstall";
            UninstallBtn.UseVisualStyleBackColor = true;
            UninstallBtn.Click += UninstallBtn_Click;
            // 
            // DiscordBtn
            // 
            DiscordBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            DiscordBtn.Cursor = Cursors.Hand;
            DiscordBtn.Location = new Point(12, 334);
            DiscordBtn.Name = "DiscordBtn";
            DiscordBtn.Size = new Size(96, 23);
            DiscordBtn.TabIndex = 2;
            DiscordBtn.Text = "Join Discord";
            DiscordBtn.UseVisualStyleBackColor = true;
            DiscordBtn.Click += DiscordBtn_Click;
            // 
            // UninstallerForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(692, 369);
            Controls.Add(DiscordBtn);
            Controls.Add(UninstallBtn);
            Controls.Add(OutputRichTextBox);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "UninstallerForm";
            ShowIcon = false;
            Text = "JKACrossplatform Uninstaller";
            FormClosed += UninstallerForm_FormClosed;
            Shown += UninstallerForm_Shown;
            ResumeLayout(false);
        }

        #endregion

        private RichTextBox OutputRichTextBox;
        private Button UninstallBtn;
        private Button DiscordBtn;
    }
}
