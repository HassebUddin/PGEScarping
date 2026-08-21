namespace PGEScarping
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        private Panel headerPanel;
        private Label titleLabel;
        private Label subtitleLabel;
        private Panel bodyPanel;
        private Panel controlsPanel;
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
            headerPanel = new Panel();
            titleLabel = new Label();
            subtitleLabel = new Label();
            bodyPanel = new Panel();
            controlsPanel = new Panel();
            btnStart = new Button();
            btnOpenFolder = new Button();
            statusLabel = new Label();
            logBox = new RichTextBox();
            progressBar = new ProgressBar();
            headerPanel.SuspendLayout();
            bodyPanel.SuspendLayout();
            controlsPanel.SuspendLayout();
            SuspendLayout();

            // headerPanel
            headerPanel.BackColor = Color.FromArgb(27, 29, 42);
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 92;
            headerPanel.Controls.Add(subtitleLabel);
            headerPanel.Controls.Add(titleLabel);

            // titleLabel
            titleLabel.AutoSize = true;
            titleLabel.BackColor = Color.Transparent;
            titleLabel.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point);
            titleLabel.ForeColor = Color.FromArgb(76, 139, 245);
            titleLabel.Location = new Point(28, 16);
            titleLabel.Text = "PG&&E Billing Automation";

            // subtitleLabel
            subtitleLabel.AutoSize = true;
            subtitleLabel.BackColor = Color.Transparent;
            subtitleLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            subtitleLabel.ForeColor = Color.FromArgb(154, 160, 180);
            subtitleLabel.Location = new Point(31, 56);
            subtitleLabel.Text = "Selenium-powered billing history scraper";

            // controlsPanel
            controlsPanel.BackColor = Color.Transparent;
            controlsPanel.Dock = DockStyle.Top;
            controlsPanel.Height = 100;
            controlsPanel.Controls.Add(statusLabel);
            controlsPanel.Controls.Add(btnOpenFolder);
            controlsPanel.Controls.Add(btnStart);

            // btnStart
            btnStart.BackColor = Color.FromArgb(76, 139, 245);
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point);
            btnStart.ForeColor = Color.White;
            btnStart.Location = new Point(0, 0);
            btnStart.Size = new Size(200, 46);
            btnStart.Text = "Start Scraping";
            btnStart.UseVisualStyleBackColor = false;
            btnStart.Click += btnStart_Click;

            // btnOpenFolder
            btnOpenFolder.BackColor = Color.FromArgb(40, 42, 58);
            btnOpenFolder.Enabled = false;
            btnOpenFolder.FlatStyle = FlatStyle.Flat;
            btnOpenFolder.FlatAppearance.BorderSize = 0;
            btnOpenFolder.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            btnOpenFolder.ForeColor = Color.FromArgb(224, 226, 235);
            btnOpenFolder.Location = new Point(216, 0);
            btnOpenFolder.Size = new Size(200, 46);
            btnOpenFolder.Text = "Open Output Folder";
            btnOpenFolder.UseVisualStyleBackColor = false;
            btnOpenFolder.Click += btnOpenFolder_Click;

            // statusLabel
            statusLabel.AutoSize = true;
            statusLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            statusLabel.ForeColor = Color.FromArgb(154, 160, 180);
            statusLabel.Location = new Point(2, 62);
            statusLabel.Text = "Ready.";

            // progressBar
            progressBar.Dock = DockStyle.Bottom;
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.MarqueeAnimationSpeed = 0;
            progressBar.Height = 6;

            // logBox
            logBox.BackColor = Color.FromArgb(13, 14, 20);
            logBox.BorderStyle = BorderStyle.FixedSingle;
            logBox.Dock = DockStyle.Fill;
            logBox.Font = new Font("Consolas", 9.75F, FontStyle.Regular, GraphicsUnit.Point);
            logBox.ForeColor = Color.FromArgb(150, 230, 170);
            logBox.ReadOnly = true;

            // bodyPanel
            bodyPanel.BackColor = Color.FromArgb(18, 19, 26);
            bodyPanel.Dock = DockStyle.Fill;
            bodyPanel.Padding = new Padding(28, 20, 28, 20);
            bodyPanel.Controls.Add(logBox);
            bodyPanel.Controls.Add(controlsPanel);

            // Form1
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(18, 19, 26);
            ClientSize = new Size(1000, 650);
            MinimumSize = new Size(760, 480);
            Controls.Add(bodyPanel);
            Controls.Add(headerPanel);
            Controls.Add(progressBar);
            Text = "PG&E Billing Automation";
            StartPosition = FormStartPosition.CenterScreen;

            headerPanel.ResumeLayout(false);
            headerPanel.PerformLayout();
            controlsPanel.ResumeLayout(false);
            controlsPanel.PerformLayout();
            bodyPanel.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
