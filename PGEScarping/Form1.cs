using PGEScarping.Enums;
using PGEScarping.Helpers;
using PGEScarping.Interfaces;
using PGEScarping.Modules;

namespace PGEScarping
{
    public partial class Form1 : Form
    {
        private readonly List<IScrapingModule> _modules;
        private readonly AppLogFile _logFile;
        private IScrapingModule? _currentModule;
        private Panel? _selectedCard;
        private bool _isRunning;
        private string _lastOutputFolder = "";
        private TaskCompletionSource<string?>? _pendingInputPrompt;

        public Form1(IEnumerable<IScrapingModule> modules, AppLogFile logFile)
        {
            InitializeComponent();
            _logFile = logFile;

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

            Load += Form1_Load;
        }

        private const string AccountNumberPlaceholder = "Required — enter the account number to process";

        private async void Form1_Load(object? sender, EventArgs e)
        {
            btnStart.Enabled = false;
            btnStartAll.Enabled = false;
            SetStatus("Initializing embedded browser...", StatusKind.Running);
            await browserView.EnsureCoreWebView2Async();
            SetStatus(_currentModule?.IsAvailable == true ? "Ready." : "This module is coming soon.", StatusKind.Ready);
            btnStart.Enabled = _currentModule?.IsAvailable == true;
            btnStartAll.Enabled = _currentModule?.IsAvailable == true;
        }

        private enum StatusKind { Ready, Running, Success, Error }

        private void SetStatus(string message, StatusKind kind)
        {
            var (dot, color) = kind switch
            {
                StatusKind.Running => ("●", UiStyleHelper.Accent),
                StatusKind.Success => ("●", UiStyleHelper.Success),
                StatusKind.Error => ("●", UiStyleHelper.Danger),
                _ => ("○", UiStyleHelper.TextSecondary)
            };

            statusLabel.Text = $"{dot}  {message}";
            statusLabel.ForeColor = color;
        }

        private void accountNumberBox_Enter(object? sender, EventArgs e)
        {
            if (accountNumberBox.Text == AccountNumberPlaceholder)
            {
                accountNumberBox.Text = "";
                accountNumberBox.ForeColor = UiStyleHelper.TextPrimary;
            }
        }

        private void accountNumberBox_Leave(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(accountNumberBox.Text))
            {
                accountNumberBox.Text = AccountNumberPlaceholder;
                accountNumberBox.ForeColor = UiStyleHelper.TextSecondary;
            }
        }

        private string? GetAccountNumberOverride()
        {
            var value = accountNumberBox.Text.Trim();
            return string.IsNullOrWhiteSpace(value) || value == AccountNumberPlaceholder ? null : value;
        }

        private Panel BuildModuleCard(IScrapingModule module)
        {
            var isAvailable = module.IsAvailable;
            var foreColor = isAvailable ? UiStyleHelper.TextPrimary : UiStyleHelper.TextSecondary;

            var card = new Panel
            {
                Width = 252,
                Height = 80,
                Margin = new Padding(0, 0, 0, 12),
                BackColor = UiStyleHelper.Surface,
                Cursor = isAvailable ? Cursors.Hand : Cursors.Default,
            };
            UiStyleHelper.ApplyRoundedCorners(card, 16);
            card.Paint += (_, e) =>
            {
                using var pen = new Pen(UiStyleHelper.Border, 1f);
                e.Graphics.DrawPath(pen, UiStyleHelper.RoundedRectPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 16));
            };

            var accentBar = new Panel
            {
                BackColor = UiStyleHelper.Accent,
                Location = new Point(0, 10),
                Size = new Size(4, card.Height - 20),
                Visible = false
            };
            card.Tag = accentBar;

            var iconBadge = new Panel
            {
                BackColor = isAvailable ? UiStyleHelper.AccentSoft : UiStyleHelper.Surface,
                Location = new Point(14, 14),
                Size = new Size(52, 52)
            };
            UiStyleHelper.ApplyRoundedCorners(iconBadge, 14);

            var iconLabel = new Label
            {
                Text = module.IconGlyph,
                Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                ForeColor = foreColor
            };
            iconBadge.Controls.Add(iconLabel);

            var nameLabel = new Label
            {
                Text = module.DisplayName,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point),
                AutoSize = true,
                Location = new Point(78, 20),
                BackColor = Color.Transparent,
                ForeColor = foreColor
            };

            var badge = new Label
            {
                Text = isAvailable ? "● ACTIVE" : "○ SOON",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold, GraphicsUnit.Point),
                AutoSize = true,
                Location = new Point(78, 46),
                BackColor = Color.Transparent,
                ForeColor = isAvailable ? UiStyleHelper.Success : UiStyleHelper.TextSecondary
            };

            card.Controls.Add(accentBar);
            card.Controls.Add(iconBadge);
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
            {
                _selectedCard.BackColor = UiStyleHelper.Surface;
                if (_selectedCard.Tag is Panel previousAccentBar)
                    previousAccentBar.Visible = false;
            }

            card.BackColor = UiStyleHelper.AccentSoft;
            if (card.Tag is Panel accentBar)
                accentBar.Visible = true;

            _selectedCard = card;
            _currentModule = module;

            moduleIconLabel.Text = module.IconGlyph;
            moduleNameLabel.Text = module.DisplayName;
            moduleDescLabel.Text = module.Description;
            SetStatus(module.IsAvailable ? "Ready." : "This module is coming soon.", StatusKind.Ready);
            btnStart.Enabled = module.IsAvailable;
            btnStartAll.Enabled = module.IsAvailable;
            btnOpenFolder.Enabled = false;
        }

        private async void btnStart_Click(object? sender, EventArgs e)
        {
            if (_currentModule is null || !_currentModule.IsAvailable)
                return;

            var accountNumberOverride = GetAccountNumberOverride();
            if (accountNumberOverride is null)
            {
                MessageBox.Show(this, "Please enter an account number before starting.", "Account number required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                accountNumberBox.Focus();
                return;
            }

            await RunScrapeAsync(accountNumberOverride);
        }

        // Runs every account discovered on the login (no override) — used to bulk-verify all ~80
        // accounts' latest bill in one pass instead of running them one at a time by hand.
        private async void btnStartAll_Click(object? sender, EventArgs e)
        {
            if (_currentModule is null || !_currentModule.IsAvailable)
                return;

            await RunScrapeAsync(accountNumberOverride: null);
        }

        private async Task RunScrapeAsync(string? accountNumberOverride)
        {
            if (_currentModule is null)
                return;

            _isRunning = true;
            btnStart.Enabled = false;
            btnStartAll.Enabled = false;
            btnOpenFolder.Enabled = false;
            SetStatus("Running...", StatusKind.Running);
            progressBar.IsRunning = true;

            var progress = new Progress<string>(AppendLog);

            try
            {
                var result = await _currentModule.RunAsync(browserView, progress, PromptForInputAsync, accountNumberOverride);

                if (result.Success)
                {
                    _lastOutputFolder = Path.GetDirectoryName(result.OutputFilePath) ?? "";
                    SetStatus(result.Message, StatusKind.Success);
                    btnOpenFolder.Enabled = !string.IsNullOrWhiteSpace(_lastOutputFolder);
                    MessageBox.Show(this, result.Message, "Scraping complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    SetStatus("Failed: " + result.Message, StatusKind.Error);
                    MessageBox.Show(this, result.Message, "Scraping failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                progressBar.IsRunning = false;
                btnStart.Enabled = true;
                btnStartAll.Enabled = true;
                _isRunning = false;
                codePromptPanel.Visible = false;
                _pendingInputPrompt = null;
            }
        }

        private void btnOpenFolder_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_lastOutputFolder) && Directory.Exists(_lastOutputFolder))
                System.Diagnostics.Process.Start("explorer.exe", _lastOutputFolder);
        }

        // Shows an inline prompt bar (instead of a separate popup Form) and waits for the user to
        // submit it. A brand new top-level Form alongside the embedded WebView2 control was found to
        // crash the whole process, so the request is answered in-place within this same window.
        private Task<string?> PromptForInputAsync(string message)
        {
            _pendingInputPrompt = new TaskCompletionSource<string?>();

            codePromptLabel.Text = message;
            codeInputBox.Text = "";
            codePromptPanel.Visible = true;
            codeInputBox.Focus();

            return _pendingInputPrompt.Task;
        }

        private void SubmitCodePrompt()
        {
            if (_pendingInputPrompt is null)
                return;

            var value = codeInputBox.Text.Trim();
            codePromptPanel.Visible = false;
            _pendingInputPrompt.TrySetResult(string.IsNullOrEmpty(value) ? null : value);
            _pendingInputPrompt = null;
        }

        private void btnSubmitCode_Click(object? sender, EventArgs e) => SubmitCodePrompt();

        private void codeInputBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            e.SuppressKeyPress = true;
            SubmitCodePrompt();
        }

        private void AppendLog(string message)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            SetStatus(message, StatusKind.Running);
            _logFile.Append(line);
        }

        private void btnViewLog_Click(object? sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_logFile.FilePath) { UseShellExecute = true });
        }
    }
}
