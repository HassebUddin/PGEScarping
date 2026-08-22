using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Web.WebView2.WinForms;
using PGEScarping.Dto;
using PGEScarping.Enums;
using PGEScarping.Helpers;
using PGEScarping.Interfaces;
using PGEScarping.Models;
using PGEScarping.Options;

namespace PGEScarping.Services;

public sealed class PgeScrapingService : IScrapingModule
{
    public ScrapingSourceType SourceType => ScrapingSourceType.PGEBilling;
    public string DisplayName => "PG&E Billing";
    public string Description => "Logs in to the PG&E customer portal, walks every linked account's full billing history, and exports the charge breakdown for each bill to Excel.";
    public string IconGlyph => "⚡";
    public bool IsAvailable => true;

    private readonly IScrapingWebsiteRepository _scrapingWebsiteRepository;
    private readonly ScrapingOptions _options;
    private readonly EmailInboxOptions _emailOptions;
    private readonly AppLogFile _logFile;
    private readonly ILogger<PgeScrapingService> _logger;

    public PgeScrapingService(
        IScrapingWebsiteRepository scrapingWebsiteRepository,
        IOptions<ScrapingOptions> options,
        IOptions<EmailInboxOptions> emailOptions,
        AppLogFile logFile,
        ILogger<PgeScrapingService> logger)
    {
        _scrapingWebsiteRepository = scrapingWebsiteRepository;
        _options = options.Value;
        _emailOptions = emailOptions.Value;
        _logFile = logFile;
        _logger = logger;
    }

    public async Task<ScrapeResult> RunAsync(
        WebView2 browser,
        IProgress<string> progress,
        Func<string, Task<string?>> promptForInputAsync,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var website = await _scrapingWebsiteRepository.GetActiveBySourceTypeIdAsync((int)SourceType);
            if (website is null)
                return new ScrapeResult { Success = false, Message = "No active PG&E website credentials found in the database." };

            progress.Report($"Opening {website.website_url} ...");
            await PgeWebViewAutomationHelper.NavigateAndWaitAsync(browser, website.website_url);

            var loginFormAppeared = await PgeWebViewAutomationHelper.WaitForShadowElementAsync(browser, "input[name='username']", timeoutMs: 20000);
            if (loginFormAppeared)
            {
                // The OneTrust cookie banner loads asynchronously after the page itself; only try to
                // dismiss it once the rest of the page (the login form) has actually rendered.
                await PgeWebViewAutomationHelper.ClickInShadowAsync(browser, "#onetrust-accept-btn-handler");
                await Task.Delay(1000, cancellationToken);

                progress.Report($"Logging in as {website.username} ...");
                var usernameFilled = await PgeWebViewAutomationHelper.SetShadowInputValueAsync(browser, "input[name='username']", website.username);
                var passwordFilled = await PgeWebViewAutomationHelper.SetShadowInputValueAsync(browser, "input[name='password']", website.password);
                var usernameValueStuck = await PgeWebViewAutomationHelper.GetShadowInputValueAsync(browser, "input[name='username']") == website.username;
                if (!usernameFilled || !passwordFilled || !usernameValueStuck)
                    return new ScrapeResult { Success = false, Message = "Could not fill the PG&E login form — the site's page structure may have changed." };

                var signInClicked = await PgeWebViewAutomationHelper.ClickInShadowAsync(browser, "button.PrimarySignInButton");
                if (!signInClicked)
                    return new ScrapeResult { Success = false, Message = "Could not find the Sign In button on the PG&E login page." };

                await Task.Delay(5000, cancellationToken);
            }
            else
            {
                progress.Report("Login form didn't appear — the session may already be signed in.");
            }

            if (!await HandleTwoFactorIfPresentAsync(browser, progress, promptForInputAsync, cancellationToken))
                return new ScrapeResult { Success = false, Message = "Could not complete the PG&E security code verification." };

            progress.Report("Login step complete.");

            var accountNumbers = await DiscoverAccountNumbersAsync(browser);
            if (accountNumbers.Count == 0)
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = "Logged in, but couldn't find an account number on the dashboard. " +
                              "The account search box's selector needs to be checked against the live page " +
                              "(look at the embedded browser panel — press F12 to inspect it)."
                };
            }

            progress.Report($"Found {accountNumbers.Count} account(s): {string.Join(", ", accountNumbers)}");

            var allBills = new List<PgeBillRecord>();

            foreach (var accountNumber in accountNumbers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress.Report($"Account {accountNumber}: switching...");
                await SwitchToAccountAsync(browser, accountNumber);

                // Build the URL off the browser's current (post-redirect) address rather than the
                // configured website_url — m.pge.com redirects to myaccount.pge.com, and a relative
                // path resolved against the original host would point at the wrong domain.
                var currentUri = new Uri(browser.CoreWebView2.Source);
                var billingHistoryUrl = new Uri(currentUri, "/myaccount/s/bill-and-payment-history").ToString();
                await PgeWebViewAutomationHelper.NavigateAndWaitAsync(browser, billingHistoryUrl);

                var pdfLinks = await CollectBillPdfLinksAsync(browser);
                progress.Report($"Account {accountNumber}: found {pdfLinks.Count} bill(s) in history.");

                foreach (var (_, rowLabel) in pdfLinks)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress.Report($"Account {accountNumber}: downloading bill ({rowLabel})...");

                    // The popup opens off-screen in its own window, so the main frame stays on the
                    // billing history table throughout — no need to re-navigate between bills.
                    var pdfBytes = await PgeWebViewAutomationHelper.ClickBillPdfLinkAndDownloadAsync(browser, rowLabel, _logFile);
                    var fileName = $"{accountNumber}_{allBills.Count + 1}.pdf";
                    var record = PdfBillParserHelper.Parse(pdfBytes, fileName, _logFile);
                    record.AccountNumber = string.IsNullOrWhiteSpace(record.AccountNumber) ? accountNumber : record.AccountNumber;

                    allBills.Add(record);
                    progress.Report($"Account {accountNumber}: parsed bill dated {record.StatementDate:MM/dd/yyyy}, total ${record.TotalBillAmount}.");
                }
            }

            Directory.CreateDirectory(_options.OutputFolder);
            var outputFilePath = Path.Combine(_options.OutputFolder, "PGE_Billing_History.xlsx");
            ExcelExportHelper.WriteWorkbook(outputFilePath, allBills);

            progress.Report($"Done. {allBills.Count} bill(s) written to {outputFilePath}.");

            return new ScrapeResult
            {
                Success = true,
                Message = $"{allBills.Count} bill(s) exported across {accountNumbers.Count} account(s).",
                OutputFilePath = outputFilePath,
                RecordCount = allBills.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PgeScrapingService.RunAsync failed");
            progress.Report($"Error: {ex.Message}");
            return new ScrapeResult { Success = false, Message = ex.Message };
        }
    }

    private async Task<bool> HandleTwoFactorIfPresentAsync(
        WebView2 browser,
        IProgress<string> progress,
        Func<string, Task<string?>> promptForInputAsync,
        CancellationToken cancellationToken)
    {
        var codeFieldSelector = "input[name*='code' i], input[placeholder*='XXXXXX' i]";

        if (!await PgeWebViewAutomationHelper.WaitForShadowElementAsync(browser, codeFieldSelector, timeoutMs: 4000))
        {
            var chosenEmail = await PgeWebViewAutomationHelper.ClickButtonByTextInShadowAsync(browser, "email");
            if (!chosenEmail)
                return true; // No MFA step appeared at all.

            progress.Report("Security code requested via email. Waiting for it to arrive...");
            await PgeWebViewAutomationHelper.WaitForShadowElementAsync(browser, codeFieldSelector, timeoutMs: 6000);
        }

        progress.Report("Waiting for you to enter the security code (check your phone/email)...");
        var code = await promptForInputAsync("PG&E sent a 6-digit security code to your phone or email. It stays valid for 1 hour — enter it below:");

        if (string.IsNullOrWhiteSpace(code))
        {
            progress.Report("No security code was entered.");
            return false;
        }

        progress.Report("Security code received, submitting...");
        await PgeWebViewAutomationHelper.SetShadowInputValueAsync(browser, codeFieldSelector, code.Trim());
        await PgeWebViewAutomationHelper.ClickButtonByTextInShadowAsync(browser, "confirm");
        await Task.Delay(6000, cancellationToken);
        return true;
    }

    // The dashboard's account search box shows the current account number as its value (e.g.
    // "2737110417-8"), confirmed against the live dashboard. Rather than locate it relative to the
    // "Account" heading (which sits in a different shadow-DOM component and isn't reachable via
    // plain parentElement/querySelector across that boundary), this matches on the value's shape,
    // which works regardless of which component actually hosts the input.
    private const string AccountSearchBoxScript = @"
function findAccountBox(root) {
  for (const el of root.querySelectorAll('input')) {
    if (el.value && /\d{5,}-\d/.test(el.value)) return el;
  }
  for (const el of root.querySelectorAll('*')) {
    if (el.shadowRoot) {
      const found = findAccountBox(el.shadowRoot);
      if (found) return found;
    }
  }
  return null;
}
";

    private async Task<List<string>> DiscoverAccountNumbersAsync(WebView2 browser)
    {
        var script = AccountSearchBoxScript + "const el = findAccountBox(document); return el ? el.value : null;";

        // The dashboard's data (including the account number) populates a moment after the page
        // itself finishes navigating, via an async fetch inside the SPA — poll instead of checking once.
        for (var attempt = 0; attempt < 15; attempt++)
        {
            var json = await PgeWebViewAutomationHelper.ExecuteJsAsync(browser, script);
            var current = JsonSerializer.Deserialize<string?>(json);
            if (!string.IsNullOrWhiteSpace(current))
                return [current];

            await Task.Delay(1000);
        }

        _logFile.Append("DiscoverAccountNumbersAsync: no account number found after 15 attempts.");
        return [];
    }

    private static async Task SwitchToAccountAsync(WebView2 browser, string accountNumber)
    {
        var script = AccountSearchBoxScript + $@"
const el = findAccountBox(document);
if (!el) return false;
const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
setter.call(el, {JsonSerializer.Serialize(accountNumber)});
el.dispatchEvent(new Event('input', {{ bubbles: true }}));
el.dispatchEvent(new KeyboardEvent('keydown', {{ key: 'Enter', bubbles: true }}));
return true;
";
        await PgeWebViewAutomationHelper.ExecuteJsAsync(browser, script);
        await Task.Delay(1500);
    }

    private const string CollectBillPdfLinksScript = PgeWebViewAutomationHelper.FindInShadowJs + @"
function collect(root, results) {
  root.querySelectorAll('tr').forEach(row => {
    const link = Array.from(row.querySelectorAll('a')).find(a => (a.innerText || '').includes('View Bill PDF'));
    if (link) results.push({ href: link.href, label: row.innerText.replace(/\n/g, ' ').trim() });
  });
  root.querySelectorAll('*').forEach(el => {
    if (el.shadowRoot) collect(el.shadowRoot, results);
  });
}
const results = [];
collect(document, results);
return JSON.stringify(results);
";

    private async Task<List<(string PdfUrl, string RowLabel)>> CollectBillPdfLinksAsync(WebView2 browser)
    {
        // Same reasoning as DiscoverAccountNumbersAsync: the billing history table populates
        // asynchronously after navigation, so a one-shot read can catch it before any rows exist.
        for (var attempt = 0; attempt < 15; attempt++)
        {
            var json = await PgeWebViewAutomationHelper.ExecuteJsAsync(browser, CollectBillPdfLinksScript);
            var raw = JsonSerializer.Deserialize<string>(json) ?? json;
            var items = JsonSerializer.Deserialize<List<PdfLinkRow>>(raw) ?? [];
            if (items.Count > 0)
                return items.Select(i => (i.href, i.label)).ToList();

            await Task.Delay(1000);
        }

        _logFile.Append("CollectBillPdfLinksAsync: no 'View Bill PDF' rows found after 15 attempts.");
        return [];
    }

    private sealed class PdfLinkRow
    {
        public string href { get; set; } = "";
        public string label { get; set; } = "";
    }
}
