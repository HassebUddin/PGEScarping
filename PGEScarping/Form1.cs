using PGEScarping.Interfaces;

namespace PGEScarping
{
    public partial class Form1 : Form
    {
        private readonly IPgeScrapingService _scrapingService;
        private string _lastOutputFolder = "";

        public Form1(IPgeScrapingService scrapingService)
        {
            InitializeComponent();
            _scrapingService = scrapingService;
        }

        private async void btnStart_Click(object? sender, EventArgs e)
        {
            btnStart.Enabled = false;
            btnOpenFolder.Enabled = false;
            logBox.Clear();
            statusLabel.Text = "Running...";
            progressBar.MarqueeAnimationSpeed = 30;

            var progress = new Progress<string>(AppendLog);

            try
            {
                var result = await _scrapingService.RunAsync(progress);

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
