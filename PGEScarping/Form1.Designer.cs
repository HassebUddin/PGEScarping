namespace PGEScarping
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        private Panel sidebarPanel;
        private Panel brandPanel;
        private Label brandTitleLabel;
        private Label brandSubtitleLabel;
        private FlowLayoutPanel moduleListPanel;

        private Panel contentPanel;
        private Panel moduleHeaderPanel;
        private Label moduleIconLabel;
        private Label moduleNameLabel;
        private Label moduleDescLabel;
        private Panel actionsPanel;
        private Button btnStart;
        private Button btnOpenFolder;
        private Label statusLabel;
        private RichTextBox logBox;
        private ProgressBar progressBar;

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
            brandTitleLabel = new Label();
            brandSubtitleLabel = new Label();
            moduleListPanel = new FlowLayoutPanel();
            contentPanel = new Panel();
            moduleHeaderPanel = new Panel();
            moduleIconLabel = new Label();
            moduleNameLabel = new Label();
            moduleDescLabel = new Label();
            actionsPanel = new Panel();
            btnStart = new Button();
            btnOpenFolder = new Button();
            statusLabel = new Label();
            logBox = new RichTextBox();
            progressBar = new ProgressBar();

            sidebarPanel.SuspendLayout();
            brandPanel.SuspendLayout();
            contentPanel.SuspendLayout();
            moduleHeaderPanel.SuspendLayout();
            actionsPanel.SuspendLayout();
            SuspendLayout();

            // brandTitleLabel
            brandTitleLabel.AutoSize = true;
            brandTitleLabel.Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point);
            brandTitleLabel.ForeColor = Helpers.UiStyleHelper.Accent;
            brandTitleLabel.Location = new Point(24, 22);
            brandTitleLabel.Text = "⚡ AUTOMATION HUB";

            // brandSubtitleLabel
            brandSubtitleLabel.AutoSize = true;
            brandSubtitleLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            brandSubtitleLabel.ForeColor = Helpers.UiStyleHelper.TextSecondary;
            brandSubtitleLabel.Location = new Point(27, 54);
            brandSubtitleLabel.Text = "Store utilities scraping suite";

            // brandPanel
            brandPanel.BackColor = Helpers.UiStyleHelper.Sidebar;
            brandPanel.Dock = DockStyle.Top;
            brandPanel.Height = 96;
            brandPanel.Controls.Add(brandSubtitleLabel);
            brandPanel.Controls.Add(brandTitleLabel);

            // moduleListPanel
            moduleListPanel.AutoScroll = true;
            moduleListPanel.BackColor = Helpers.UiStyleHelper.Sidebar;
            moduleListPanel.Dock = DockStyle.Fill;
            moduleListPanel.FlowDirection = FlowDirection.TopDown;
            moduleListPanel.Padding = new Padding(16, 16, 16, 16);
            moduleListPanel.WrapContents = false;

            // sidebarPanel
            sidebarPanel.BackColor = Helpers.UiStyleHelper.Sidebar;
            sidebarPanel.Dock = DockStyle.Left;
            sidebarPanel.Width = 300;
            sidebarPanel.Controls.Add(moduleListPanel);
            sidebarPanel.Controls.Add(brandPanel);

            // moduleIconLabel
            moduleIconLabel.AutoSize = true;
            moduleIconLabel.Font = new Font("Segoe UI", 26F, FontStyle.Regular, GraphicsUnit.Point);
            moduleIconLabel.Location = new Point(0, 4);
            moduleIconLabel.Text = "⚡";

            // moduleNameLabel
            moduleNameLabel.AutoSize = true;
            moduleNameLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
            moduleNameLabel.ForeColor = Helpers.UiStyleHelper.TextPrimary;
            moduleNameLabel.Location = new Point(56, 2);
            moduleNameLabel.Text = "PG&&E Billing";

            // moduleDescLabel
            moduleDescLabel.AutoSize = false;
            moduleDescLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            moduleDescLabel.ForeColor = Helpers.UiStyleHelper.TextSecondary;
            moduleDescLabel.Location = new Point(58, 38);
            moduleDescLabel.Size = new Size(600, 40);
            moduleDescLabel.Text = "";

            // moduleHeaderPanel
            moduleHeaderPanel.BackColor = Color.Transparent;
            moduleHeaderPanel.Dock = DockStyle.Top;
            moduleHeaderPanel.Height = 92;
            moduleHeaderPanel.Controls.Add(moduleDescLabel);
            moduleHeaderPanel.Controls.Add(moduleNameLabel);
            moduleHeaderPanel.Controls.Add(moduleIconLabel);

            // btnStart
            btnStart.BackColor = Helpers.UiStyleHelper.Accent;
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point);
            btnStart.ForeColor = Color.White;
            btnStart.Location = new Point(0, 8);
            btnStart.Size = new Size(190, 44);
            btnStart.Text = "▶  Start Scraping";
            btnStart.UseVisualStyleBackColor = false;
            btnStart.Click += btnStart_Click;

            // btnOpenFolder
            btnOpenFolder.BackColor = Helpers.UiStyleHelper.Surface;
            btnOpenFolder.Enabled = false;
            btnOpenFolder.FlatStyle = FlatStyle.Flat;
            btnOpenFolder.FlatAppearance.BorderSize = 0;
            btnOpenFolder.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
            btnOpenFolder.ForeColor = Helpers.UiStyleHelper.TextPrimary;
            btnOpenFolder.Location = new Point(206, 8);
            btnOpenFolder.Size = new Size(190, 44);
            btnOpenFolder.Text = "📁  Open Output Folder";
            btnOpenFolder.UseVisualStyleBackColor = false;
            btnOpenFolder.Click += btnOpenFolder_Click;

            // statusLabel
            statusLabel.AutoSize = true;
            statusLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            statusLabel.ForeColor = Helpers.UiStyleHelper.TextSecondary;
            statusLabel.Location = new Point(2, 56);
            statusLabel.Text = "Ready.";

            // actionsPanel
            actionsPanel.BackColor = Color.Transparent;
            actionsPanel.Dock = DockStyle.Top;
            actionsPanel.Height = 76;
            actionsPanel.Controls.Add(statusLabel);
            actionsPanel.Controls.Add(btnOpenFolder);
            actionsPanel.Controls.Add(btnStart);

            // logBox
            logBox.BackColor = Color.FromArgb(10, 11, 16);
            logBox.BorderStyle = BorderStyle.FixedSingle;
            logBox.Dock = DockStyle.Fill;
            logBox.Font = new Font("Consolas", 9.75F, FontStyle.Regular, GraphicsUnit.Point);
            logBox.ForeColor = Helpers.UiStyleHelper.LogText;
            logBox.ReadOnly = true;

            // contentPanel
            contentPanel.BackColor = Helpers.UiStyleHelper.Background;
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Padding = new Padding(32, 24, 32, 24);
            contentPanel.Controls.Add(logBox);
            contentPanel.Controls.Add(actionsPanel);
            contentPanel.Controls.Add(moduleHeaderPanel);

            // progressBar
            progressBar.Dock = DockStyle.Bottom;
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.MarqueeAnimationSpeed = 0;
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
            brandPanel.PerformLayout();
            contentPanel.ResumeLayout(false);
            moduleHeaderPanel.ResumeLayout(false);
            moduleHeaderPanel.PerformLayout();
            actionsPanel.ResumeLayout(false);
            actionsPanel.PerformLayout();
            ResumeLayout(false);
        }
    }
}
