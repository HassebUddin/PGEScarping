namespace PGEScarping
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        private Panel sidebarPanel;
        private Panel brandPanel;
        private Label brandLogoLabel;
        private Label brandTitleLabel;
        private Label brandSubtitleLabel;
        private FlowLayoutPanel moduleListPanel;

        private Panel contentPanel;
        private Panel moduleHeaderPanel;
        private Panel moduleIconBadge;
        private Label moduleIconLabel;
        private Label moduleNameLabel;
        private Label moduleDescLabel;
        private Panel actionsPanel;
        private Helpers.GradientButton btnStart;
        private Helpers.GradientButton btnOpenFolder;
        private Helpers.GradientButton btnViewLog;
        private Label statusLabel;
        private Label accountNumberCaptionLabel;
        private Panel accountNumberBoxWrapper;
        private TextBox accountNumberBox;
        private Panel codePromptPanel;
        private Label codePromptLabel;
        private Panel codeInputBoxWrapper;
        private TextBox codeInputBox;
        private Helpers.GradientButton btnSubmitCode;
        private Microsoft.Web.WebView2.WinForms.WebView2 browserView;
        private Helpers.PillProgressBar progressBar;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            sidebarPanel = new Panel();
            brandPanel = new Panel();
            brandLogoLabel = new Label();
            brandTitleLabel = new Label();
            brandSubtitleLabel = new Label();
            moduleListPanel = new FlowLayoutPanel();
            contentPanel = new Panel();
            moduleHeaderPanel = new Panel();
            moduleIconBadge = new Panel();
            moduleIconLabel = new Label();
            moduleNameLabel = new Label();
            moduleDescLabel = new Label();
            actionsPanel = new Panel();
            btnStart = new Helpers.GradientButton();
            btnOpenFolder = new Helpers.GradientButton();
            btnViewLog = new Helpers.GradientButton();
            statusLabel = new Label();
            accountNumberCaptionLabel = new Label();
            accountNumberBoxWrapper = new Panel();
            accountNumberBox = new TextBox();
            codePromptPanel = new Panel();
            codePromptLabel = new Label();
            codeInputBoxWrapper = new Panel();
            codeInputBox = new TextBox();
            btnSubmitCode = new Helpers.GradientButton();
            browserView = new Microsoft.Web.WebView2.WinForms.WebView2();
            progressBar = new Helpers.PillProgressBar();

            sidebarPanel.SuspendLayout();
            brandPanel.SuspendLayout();
            contentPanel.SuspendLayout();
            moduleHeaderPanel.SuspendLayout();
            moduleIconBadge.SuspendLayout();
            actionsPanel.SuspendLayout();
            accountNumberBoxWrapper.SuspendLayout();
            codePromptPanel.SuspendLayout();
            codeInputBoxWrapper.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)browserView).BeginInit();
            SuspendLayout();

            // brandLogoLabel
            brandLogoLabel.AutoSize = false;
            brandLogoLabel.BackColor = Color.Transparent;
            brandLogoLabel.Font = new Font("Segoe UI", 15F, FontStyle.Bold, GraphicsUnit.Point);
            brandLogoLabel.ForeColor = Color.White;
            brandLogoLabel.Location = new Point(24, 20);
            brandLogoLabel.Size = new Size(44, 44);
            brandLogoLabel.Text = "⚡";
            brandLogoLabel.TextAlign = ContentAlignment.MiddleCenter;

            // brandTitleLabel
            brandTitleLabel.AutoSize = true;
            brandTitleLabel.Font = new Font("Segoe UI", 12.5F, FontStyle.Bold, GraphicsUnit.Point);
            brandTitleLabel.ForeColor = Helpers.UiStyleHelper.TextPrimary;
            brandTitleLabel.Location = new Point(80, 24);
            brandTitleLabel.Text = "AUTOMATION HUB";

            // brandSubtitleLabel
            brandSubtitleLabel.AutoSize = true;
            brandSubtitleLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            brandSubtitleLabel.ForeColor = Helpers.UiStyleHelper.TextSecondary;
            brandSubtitleLabel.Location = new Point(82, 50);
            brandSubtitleLabel.Text = "Store utilities scraping suite";

            // brandPanel
            brandPanel.Dock = DockStyle.Top;
            brandPanel.Height = 100;
            brandPanel.Controls.Add(brandSubtitleLabel);
            brandPanel.Controls.Add(brandTitleLabel);
            brandPanel.Controls.Add(brandLogoLabel);
            Helpers.UiStyleHelper.PaintVerticalGradient(brandPanel, Helpers.UiStyleHelper.Sidebar, Helpers.UiStyleHelper.SidebarDeep);
            Helpers.UiStyleHelper.ApplyRoundedCorners(brandLogoLabel, 12);
            brandLogoLabel.Paint += (_, e) =>
            {
                using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    brandLogoLabel.ClientRectangle, Helpers.UiStyleHelper.AccentStart, Helpers.UiStyleHelper.AccentEnd,
                    System.Drawing.Drawing2D.LinearGradientMode.ForwardDiagonal);
                e.Graphics.FillRectangle(brush, brandLogoLabel.ClientRectangle);
                TextRenderer.DrawText(e.Graphics, brandLogoLabel.Text, brandLogoLabel.Font, brandLogoLabel.ClientRectangle,
                    Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            // moduleListPanel
            moduleListPanel.AutoScroll = true;
            moduleListPanel.BackColor = Helpers.UiStyleHelper.Sidebar;
            moduleListPanel.Dock = DockStyle.Fill;
            moduleListPanel.FlowDirection = FlowDirection.TopDown;
            moduleListPanel.Padding = new Padding(16, 20, 16, 16);
            moduleListPanel.WrapContents = false;

            // sidebarPanel
            sidebarPanel.BackColor = Helpers.UiStyleHelper.Sidebar;
            sidebarPanel.Dock = DockStyle.Left;
            sidebarPanel.Width = 300;
            sidebarPanel.Controls.Add(moduleListPanel);
            sidebarPanel.Controls.Add(brandPanel);
            sidebarPanel.Paint += (_, e) =>
            {
                using var pen = new Pen(Helpers.UiStyleHelper.Border, 1f);
                e.Graphics.DrawLine(pen, sidebarPanel.Width - 1, 0, sidebarPanel.Width - 1, sidebarPanel.Height);
            };

            // moduleIconLabel
            moduleIconLabel.AutoSize = false;
            moduleIconLabel.Dock = DockStyle.Fill;
            moduleIconLabel.Font = new Font("Segoe UI", 24F, FontStyle.Regular, GraphicsUnit.Point);
            moduleIconLabel.TextAlign = ContentAlignment.MiddleCenter;
            moduleIconLabel.BackColor = Color.Transparent;
            moduleIconLabel.Text = "⚡";

            // moduleIconBadge
            moduleIconBadge.BackColor = Helpers.UiStyleHelper.AccentSoft;
            moduleIconBadge.Location = new Point(0, 4);
            moduleIconBadge.Size = new Size(64, 64);
            moduleIconBadge.Controls.Add(moduleIconLabel);
            Helpers.UiStyleHelper.ApplyRoundedCorners(moduleIconBadge, 18);

            // moduleNameLabel
            moduleNameLabel.AutoSize = true;
            moduleNameLabel.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point);
            moduleNameLabel.ForeColor = Helpers.UiStyleHelper.TextPrimary;
            moduleNameLabel.Location = new Point(80, 2);
            moduleNameLabel.Text = "PG&&E Billing";

            // moduleDescLabel
            moduleDescLabel.AutoSize = false;
            moduleDescLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            moduleDescLabel.ForeColor = Helpers.UiStyleHelper.TextSecondary;
            moduleDescLabel.Location = new Point(82, 42);
            moduleDescLabel.Size = new Size(700, 40);
            moduleDescLabel.Text = "";

            // moduleHeaderPanel
            moduleHeaderPanel.BackColor = Color.Transparent;
            moduleHeaderPanel.Dock = DockStyle.Top;
            moduleHeaderPanel.Height = 92;
            moduleHeaderPanel.Controls.Add(moduleDescLabel);
            moduleHeaderPanel.Controls.Add(moduleNameLabel);
            moduleHeaderPanel.Controls.Add(moduleIconBadge);

            // btnStart
            btnStart.ColorStart = Helpers.UiStyleHelper.AccentStart;
            btnStart.ColorEnd = Helpers.UiStyleHelper.AccentEnd;
            btnStart.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point);
            btnStart.IconGlyph = "▶";
            btnStart.Location = new Point(0, 8);
            btnStart.Size = new Size(190, 44);
            btnStart.Text = "Start Scraping";
            btnStart.Click += btnStart_Click;

            // btnOpenFolder
            btnOpenFolder.ColorStart = Helpers.UiStyleHelper.Surface;
            btnOpenFolder.ColorEnd = Helpers.UiStyleHelper.Surface;
            btnOpenFolder.TextColor = Helpers.UiStyleHelper.TextPrimary;
            btnOpenFolder.ShowBorder = true;
            btnOpenFolder.Enabled = false;
            btnOpenFolder.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
            btnOpenFolder.IconGlyph = "📁";
            btnOpenFolder.Location = new Point(206, 8);
            btnOpenFolder.Size = new Size(190, 44);
            btnOpenFolder.Text = "Open Output Folder";
            btnOpenFolder.Click += btnOpenFolder_Click;

            // btnViewLog
            btnViewLog.ColorStart = Helpers.UiStyleHelper.Surface;
            btnViewLog.ColorEnd = Helpers.UiStyleHelper.Surface;
            btnViewLog.TextColor = Helpers.UiStyleHelper.TextPrimary;
            btnViewLog.ShowBorder = true;
            btnViewLog.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
            btnViewLog.IconGlyph = "📝";
            btnViewLog.Location = new Point(412, 8);
            btnViewLog.Size = new Size(160, 44);
            btnViewLog.Text = "View Log File";
            btnViewLog.Click += btnViewLog_Click;

            // statusLabel
            statusLabel.AutoSize = true;
            statusLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point);
            statusLabel.ForeColor = Helpers.UiStyleHelper.TextSecondary;
            statusLabel.Location = new Point(2, 60);
            statusLabel.Text = "●  Ready.";

            // accountNumberCaptionLabel
            accountNumberCaptionLabel.AutoSize = true;
            accountNumberCaptionLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            accountNumberCaptionLabel.ForeColor = Helpers.UiStyleHelper.TextSecondary;
            accountNumberCaptionLabel.Location = new Point(2, 90);
            accountNumberCaptionLabel.Text = "Account number (required):";

            // accountNumberBox
            accountNumberBox.BackColor = Helpers.UiStyleHelper.Surface;
            accountNumberBox.BorderStyle = BorderStyle.None;
            accountNumberBox.Dock = DockStyle.Fill;
            accountNumberBox.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            accountNumberBox.ForeColor = Helpers.UiStyleHelper.TextSecondary;
            accountNumberBox.Text = "Required — enter the account number to process";
            accountNumberBox.Enter += accountNumberBox_Enter;
            accountNumberBox.Leave += accountNumberBox_Leave;

            // accountNumberBoxWrapper
            accountNumberBoxWrapper.BackColor = Helpers.UiStyleHelper.Surface;
            accountNumberBoxWrapper.Location = new Point(190, 84);
            accountNumberBoxWrapper.Size = new Size(340, 32);
            accountNumberBoxWrapper.Padding = new Padding(10, 6, 10, 6);
            accountNumberBoxWrapper.Controls.Add(accountNumberBox);
            Helpers.UiStyleHelper.ApplyRoundedCorners(accountNumberBoxWrapper, 8);
            accountNumberBoxWrapper.Paint += (_, e) =>
            {
                using var pen = new Pen(Helpers.UiStyleHelper.Border, 1f);
                e.Graphics.DrawPath(pen, Helpers.UiStyleHelper.RoundedRectPath(new Rectangle(0, 0, accountNumberBoxWrapper.Width - 1, accountNumberBoxWrapper.Height - 1), 8));
            };

            // actionsPanel
            actionsPanel.BackColor = Color.Transparent;
            actionsPanel.Dock = DockStyle.Top;
            actionsPanel.Height = 128;
            actionsPanel.Controls.Add(accountNumberBoxWrapper);
            actionsPanel.Controls.Add(accountNumberCaptionLabel);
            actionsPanel.Controls.Add(statusLabel);
            actionsPanel.Controls.Add(btnViewLog);
            actionsPanel.Controls.Add(btnOpenFolder);
            actionsPanel.Controls.Add(btnStart);

            // codePromptLabel
            codePromptLabel.AutoSize = false;
            codePromptLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            codePromptLabel.ForeColor = Helpers.UiStyleHelper.TextPrimary;
            codePromptLabel.Location = new Point(0, 10);
            codePromptLabel.Size = new Size(700, 40);
            codePromptLabel.Text = "";

            // codeInputBox
            codeInputBox.BackColor = Helpers.UiStyleHelper.Surface;
            codeInputBox.BorderStyle = BorderStyle.None;
            codeInputBox.Dock = DockStyle.Fill;
            codeInputBox.Font = new Font("Consolas", 13F, FontStyle.Bold, GraphicsUnit.Point);
            codeInputBox.ForeColor = Helpers.UiStyleHelper.TextPrimary;
            codeInputBox.MaxLength = 6;
            codeInputBox.TextAlign = HorizontalAlignment.Center;
            codeInputBox.KeyDown += codeInputBox_KeyDown;

            // codeInputBoxWrapper
            codeInputBoxWrapper.BackColor = Helpers.UiStyleHelper.Surface;
            codeInputBoxWrapper.Location = new Point(710, 12);
            codeInputBoxWrapper.Size = new Size(120, 36);
            codeInputBoxWrapper.Padding = new Padding(8, 4, 8, 4);
            codeInputBoxWrapper.Controls.Add(codeInputBox);
            Helpers.UiStyleHelper.ApplyRoundedCorners(codeInputBoxWrapper, 8);
            codeInputBoxWrapper.Paint += (_, e) =>
            {
                using var pen = new Pen(Helpers.UiStyleHelper.Accent, 1.4f);
                e.Graphics.DrawPath(pen, Helpers.UiStyleHelper.RoundedRectPath(new Rectangle(0, 0, codeInputBoxWrapper.Width - 1, codeInputBoxWrapper.Height - 1), 8));
            };

            // btnSubmitCode
            btnSubmitCode.ColorStart = Helpers.UiStyleHelper.AccentStart;
            btnSubmitCode.ColorEnd = Helpers.UiStyleHelper.AccentEnd;
            btnSubmitCode.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            btnSubmitCode.Location = new Point(846, 12);
            btnSubmitCode.Size = new Size(120, 36);
            btnSubmitCode.Text = "Submit";
            btnSubmitCode.Click += btnSubmitCode_Click;

            // codePromptPanel
            codePromptPanel.BackColor = Helpers.UiStyleHelper.AccentSoft;
            codePromptPanel.Dock = DockStyle.Top;
            codePromptPanel.Height = 60;
            codePromptPanel.Padding = new Padding(16, 0, 0, 0);
            codePromptPanel.Visible = false;
            codePromptPanel.Controls.Add(btnSubmitCode);
            codePromptPanel.Controls.Add(codeInputBoxWrapper);
            codePromptPanel.Controls.Add(codePromptLabel);

            // browserView
            browserView.BackColor = Color.White;
            browserView.CreationProperties = null;
            browserView.DefaultBackgroundColor = Color.White;
            browserView.Dock = DockStyle.Fill;

            // contentPanel
            contentPanel.BackColor = Helpers.UiStyleHelper.Background;
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Padding = new Padding(32, 24, 32, 24);
            contentPanel.Controls.Add(browserView);
            contentPanel.Controls.Add(codePromptPanel);
            contentPanel.Controls.Add(actionsPanel);
            contentPanel.Controls.Add(moduleHeaderPanel);

            // progressBar
            progressBar.Dock = DockStyle.Bottom;
            progressBar.Height = 4;

            // Form1
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Helpers.UiStyleHelper.Background;
            ClientSize = new Size(1180, 700);
            MinimumSize = new Size(900, 560);
            Controls.Add(contentPanel);
            Controls.Add(sidebarPanel);
            Controls.Add(progressBar);
            Text = "Store Automation Hub";
            StartPosition = FormStartPosition.CenterScreen;

            sidebarPanel.ResumeLayout(false);
            brandPanel.ResumeLayout(false);
            contentPanel.ResumeLayout(false);
            moduleHeaderPanel.ResumeLayout(false);
            moduleHeaderPanel.PerformLayout();
            moduleIconBadge.ResumeLayout(false);
            actionsPanel.ResumeLayout(false);
            actionsPanel.PerformLayout();
            accountNumberBoxWrapper.ResumeLayout(false);
            accountNumberBoxWrapper.PerformLayout();
            codePromptPanel.ResumeLayout(false);
            codePromptPanel.PerformLayout();
            codeInputBoxWrapper.ResumeLayout(false);
            codeInputBoxWrapper.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)browserView).EndInit();
            ResumeLayout(false);
        }
    }
}
