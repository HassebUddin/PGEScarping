using PGEScarping.Enums;
using PGEScarping.Helpers;
using PGEScarping.Interfaces;
using PGEScarping.Modules;

namespace PGEScarping
{
    public partial class Form1 : Form
    {
        private readonly List<IScrapingModule> _modules;
        private IScrapingModule? _currentModule;
        private Panel? _selectedCard;
        private bool _isRunning;
        private string _lastOutputFolder = "";

        public Form1(IEnumerable<IScrapingModule> modules)
        {
            InitializeComponent();

            _modules = modules.ToList();
            _modules.Add(new ComingSoonModule(ScrapingSourceType.InternetBilling, "Internet Billing", "🌐",
                "Provider account login and billing history scraping — coming soon."));
            _modules.Add(new ComingSoonModule(ScrapingSourceType.CableBilling, "Cable Billing", "📺",
                "Provider account login and billing history scraping — coming soon."));

            Panel? firstCard = null;
            foreach (var module in _modules)
            {
                var card = BuildModuleCard(module);
                moduleListPanel.Controls.Add(card);
                firstCard ??= card;
            }

            if (firstCard is not null)
                SelectModule(_modules[0], firstCard);
        }

        private Panel BuildModuleCard(IScrapingModule module)
        {
            var isAvailable = module.IsAvailable;
            var foreColor = isAvailable ? UiStyleHelper.TextPrimary : UiStyleHelper.TextSecondary;

            var card = new Panel
            {
                Width = 252,
                Height = 76,
                Margin = new Padding(0, 0, 0, 12),
                BackColor = UiStyleHelper.Surface,
                Cursor = isAvailable ? Cursors.Hand : Cursors.Default,
            };
            UiStyleHelper.ApplyRoundedCorners(card, 14);

            var iconLabel = new Label
            {
                Text = module.IconGlyph,
                Font = new Font("Segoe UI", 20F, FontStyle.Regular, GraphicsUnit.Point),
                AutoSize = true,
                Location = new Point(16, 16),
                BackColor = Color.Transparent,
                ForeColor = foreColor
            };

            var nameLabel = new Label
            {
                Text = module.DisplayName,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point),
                AutoSize = true,
                Location = new Point(58, 14),
                BackColor = Color.Transparent,
                ForeColor = foreColor
            };

            var badge = new Label
            {
                Text = isAvailable ? "● ACTIVE" : "○ SOON",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold, GraphicsUnit.Point),
                AutoSize = true,
                Location = new Point(58, 40),
                BackColor = Color.Transparent,
                ForeColor = isAvailable ? UiStyleHelper.Success : UiStyleHelper.TextSecondary
            };

            card.Controls.Add(iconLabel);
            card.Controls.Add(nameLabel);
            card.Controls.Add(badge);

            void SelectHandler(object? s, EventArgs e) => SelectModule(module, card);
            card.Click += SelectHandler;
            iconLabel.Click += SelectHandler;
            nameLabel.Click += SelectHandler;
            badge.Click += SelectHandler;

            card.MouseEnter += (_, _) =>
            {
                if (_selectedCard != card)
                    card.BackColor = UiStyleHelper.SurfaceHover;
            };
            card.MouseLeave += (_, _) =>
            {
                if (_selectedCard != card)
                    card.BackColor = UiStyleHelper.Surface;
            };

            return card;
        }

        private void SelectModule(IScrapingModule module, Panel card)
        {
            if (_isRunning)
                return;

            if (_selectedCard is not null)
                _selectedCard.BackColor = UiStyleHelper.Surface;

            card.BackColor = UiStyleHelper.AccentSoft;
            _selectedCard = card;
            _currentModule = module;

            moduleIconLabel.Text = module.IconGlyph;
            moduleNameLabel.Text = module.DisplayName;
            moduleDescLabel.Text = module.Description;
            logBox.Clear();
            statusLabel.Text = module.IsAvailable ? "Ready." : "This module is coming soon.";
            btnStart.Enabled = module.IsAvailable;
            btnOpenFolder.Enabled = false;
        }

        private async void btnStart_Click(object? sender, EventArgs e)
        {
            if (_currentModule is null || !_currentModule.IsAvailable)
                return;

            _isRunning = true;
            btnStart.Enabled = false;
            btnOpenFolder.Enabled = false;
            logBox.Clear();
            statusLabel.Text = "Running...";
            progressBar.MarqueeAnimationSpeed = 30;

            var progress = new Progress<string>(AppendLog);

            try
            {
                var result = await _currentModule.RunAsync(progress);

                if (result.Success)
                {
                    _lastOutputFolder = Path.GetDirectoryName(result.OutputFilePath) ?? "";
                    statusLabel.Text = result.Message;
                    btnOpenFolder.Enabled = !string.IsNullOrWhiteSpace(_lastOutputFolder);
                    MessageBox.Show(this, result.Message, "Scraping complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    statusLabel.Text = "Failed: " + result.Message;
                    MessageBox.Show(this, result.Message, "Scraping failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                progressBar.MarqueeAnimationSpeed = 0;
                btnStart.Enabled = true;
                _isRunning = false;
            }
        }

        private void btnOpenFolder_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_lastOutputFolder) && Directory.Exists(_lastOutputFolder))
                System.Diagnostics.Process.Start("explorer.exe", _lastOutputFolder);
        }

        private void AppendLog(string message)
        {
            logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            logBox.SelectionStart = logBox.Text.Length;
            logBox.ScrollToCaret();
        }
    }
}
