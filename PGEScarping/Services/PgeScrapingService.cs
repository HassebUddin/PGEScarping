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
        string? accountNumberOverride = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // The same WebView2 control (and its session cookies) is reused across runs within one
            // app session — if a previous run already got us logged in and sitting on the Billing and
            // Payment History page, re-navigating to the site's home page and re-running the whole
            // login/MFA flow is pure wasted time (and re-triggers a fresh MFA prompt PG&E doesn't even
            // need again). Skip straight to the account-processing loop whenever we're already there.
            var alreadyOnBillingHistoryPage = browser.CoreWebView2?.Source?.Contains("/bill-and-payment-history", StringComparison.OrdinalIgnoreCase) == true;

            if (alreadyOnBillingHistoryPage)
            {
                progress.Report("Already logged in and on the Billing and Payment History page — skipping login.");
            }
            else
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
            }

            var accountNumbers = string.IsNullOrWhiteSpace(accountNumberOverride)
                ? await DiscoverAccountNumbersAsync(browser)
                : [accountNumberOverride.Trim()];
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

                // Each account is isolated in its own try/catch: a single account timing out or
                // erroring out (a slow PDF generation, an unexpected page state, etc.) must not abort
                // the whole run and lose every other account's already-collected/still-pending data —
                // it's logged and skipped so the loop moves on to the next account instead.
                try
                {
                    // The account search/switch box only actually changes which account's data loads
                    // when used on the Billing and Payment History page itself (confirmed against the
                    // live site) — switching on the dashboard first and then navigating here resets
                    // back to the default account, since the navigation remounts the page. So this
                    // navigates first and does the account switch on this page afterward. Already being
                    // on that exact page (e.g. right after skipping login on a re-run) still re-navigates
                    // to it — a fresh load of the same page — since a reliable reload is cheap and it
                    // guarantees a known-clean starting state before the account switch below.
                    var currentUri = new Uri(browser.CoreWebView2!.Source);
                    var billingHistoryUrl = new Uri(currentUri, "/myaccount/s/bill-and-payment-history").ToString();
                    await PgeWebViewAutomationHelper.NavigateAndWaitAsync(browser, billingHistoryUrl);

                    progress.Report($"Account {accountNumber}: switching...");
                    await WaitForAccountSearchBoxAsync(browser);
                    var switched = await SwitchToAccountAsync(browser, accountNumber);
                    if (!switched && !string.IsNullOrWhiteSpace(accountNumberOverride))
                    {
                        return new ScrapeResult
                        {
                            Success = false,
                            Message = $"Account number \"{accountNumber}\" was not found on this login. Please double-check it and try again."
                        };
                    }

                    // Only the current/latest bill for this account is wanted — the billing history
                    // table lists rows newest-first, so the first "View Bill PDF" row on page 1 is that
                    // bill. No pagination needed.
                    var currentPageItems = await CollectCurrentPageBillPdfLinksAsync(browser);
                    var pdfLinks = currentPageItems.Count > 0
                        ? new List<(string PdfUrl, string RowLabel)> { currentPageItems[0] }
                        : [];
                    progress.Report($"Account {accountNumber}: found {pdfLinks.Count} bill(s) to download.");

                    foreach (var (_, rowLabel) in pdfLinks)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        progress.Report($"Account {accountNumber}: downloading bill ({rowLabel})...");

                        // The popup runs hidden in the background, so the main frame stays on the
                        // billing history table throughout — no need to re-navigate between bills.
                        var pdfBytes = await PgeWebViewAutomationHelper.ClickBillPdfLinkAndDownloadAsync(browser, rowLabel, _logFile);
                        var fileName = $"{accountNumber}_{allBills.Count + 1}.pdf";
                        var record = PdfBillParserHelper.Parse(pdfBytes, fileName, _logFile);
                        record.AccountNumber = string.IsNullOrWhiteSpace(record.AccountNumber) ? accountNumber : record.AccountNumber;

                        allBills.Add(record);
                        progress.Report($"Account {accountNumber}: parsed bill dated {record.StatementDate:MM/dd/yyyy}, total ${record.TotalBillAmount}.");
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "PgeScrapingService: account {AccountNumber} failed, skipping it", accountNumber);
                    _logFile.Append($"Account {accountNumber}: failed and was skipped — {ex.Message}");
                    progress.Report($"Account {accountNumber}: failed ({ex.Message}) — skipping to the next account.");
                }
            }

            Directory.CreateDirectory(_options.OutputFolder);

            // A fresh, uniquely-named file per run (instead of merging into one shared workbook) so
            // each run's export stands on its own.
            var outputFileName = $"PGE_Billing_History_{DateTime.Now:yyyy-MM-dd_HHmmss}.xlsx";
            var outputFilePath = Path.Combine(_options.OutputFolder, outputFileName);
            ExcelExportHelper.WriteWorkbook(outputFilePath, allBills);

            progress.Report($"Done. {allBills.Count} bill(s) scraped this run, saved to {outputFilePath}.");

            return new ScrapeResult
            {
                Success = true,
                Message = $"{allBills.Count} bill(s) exported across {accountNumbers.Count} account(s) to {outputFileName}.",
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

    // Right after login the dashboard shows a "Your personal dashboard is almost ready!" loading
    // screen for a few seconds before the real page (with the account search box) renders — trying to
    // switch accounts during that window finds no search box at all and reports the account as "not
    // found" even though it's valid. This polls until the box actually exists (any value, including
    // still-empty) before the account-switching logic ever runs.
    private async Task WaitForAccountSearchBoxAsync(WebView2 browser, int timeoutMs = 30000, int pollIntervalMs = 1000)
    {
        var script = AccountSearchBoxScript + "return !!findAccountBox(document);";
        var elapsed = 0;
        while (elapsed < timeoutMs)
        {
            var result = await PgeWebViewAutomationHelper.ExecuteJsAsync(browser, script);
            if (result.Trim('"') == "true")
                return;

            await Task.Delay(pollIntervalMs);
            elapsed += pollIntervalMs;
        }

        _logFile.Append("WaitForAccountSearchBoxAsync: account search box never appeared within the timeout.");
    }

    // The account search box is a combobox: clicking/focusing it opens a listbox of every linked
    // account, each rendered as an option whose text contains the account number. This looks for
    // that listbox first (covering logins with more than one account) and only falls back to
    // whatever account is currently loaded if no such listbox ever appears (a single-account login,
    // or a page structure change).
    private const string FindAccountOptionsJs = @"
function findAccountOptions(root, results) {
  root.querySelectorAll('[role=""option""], li, .slds-listbox__option').forEach(el => {
    const t = (el.innerText || el.textContent || '').trim();
    const m = t.match(/\d{5,}-\d/);
    if (m) results.push(m[0]);
  });
  root.querySelectorAll('*').forEach(el => {
    if (el.shadowRoot) findAccountOptions(el.shadowRoot, results);
  });
}
";

    private async Task<List<string>> DiscoverAccountNumbersAsync(WebView2 browser)
    {
        var openDropdownScript = AccountSearchBoxScript + @"
const el = findAccountBox(document);
if (!el) return false;
el.focus();
el.dispatchEvent(new Event('focus', { bubbles: true }));
el.click();
return true;
";
        var opened = await PgeWebViewAutomationHelper.ExecuteJsAsync(browser, openDropdownScript);
        if (opened.Trim('"') == "true")
        {
            await Task.Delay(1200);

            var collectScript = FindAccountOptionsJs + @"
const results = [];
findAccountOptions(document, results);
return JSON.stringify([...new Set(results)]);
";
            var json = await PgeWebViewAutomationHelper.ExecuteJsAsync(browser, collectScript);
            var raw = JsonSerializer.Deserialize<string>(json) ?? json;
            var options = JsonSerializer.Deserialize<List<string>>(raw) ?? [];
            if (options.Count > 0)
            {
                _logFile.Append($"DiscoverAccountNumbersAsync: found {options.Count} account(s) via dropdown: {string.Join(", ", options)}");
                return options;
            }
        }

        _logFile.Append("DiscoverAccountNumbersAsync: no account dropdown/listbox found, falling back to the single currently-loaded account.");

        var currentValueScript = AccountSearchBoxScript + "const el = findAccountBox(document); return el ? el.value : null;";

        // The dashboard's data (including the account number) populates a moment after the page
        // itself finishes navigating, via an async fetch inside the SPA — poll instead of checking once.
        for (var attempt = 0; attempt < 15; attempt++)
        {
            var json = await PgeWebViewAutomationHelper.ExecuteJsAsync(browser, currentValueScript);
            var current = JsonSerializer.Deserialize<string?>(json);
            if (!string.IsNullOrWhiteSpace(current))
                return [current];

            await Task.Delay(1000);
        }

        _logFile.Append("DiscoverAccountNumbersAsync: no account number found after 15 attempts.");
        return [];
    }

    // The account listbox markup is confirmed via live devtools inspection:
    //   <input ... aria-controls=""account-listbox-524"">
    //   <div id="account-listbox-524" role="listbox">
    //     <li role="option" id="acc-0335152071-524" data-id="0335152071">
    //       <span class="acc-name" title="0335152071-7">0335152071-7</span>
    //     </li>
    //   </div>
    // Critically, the <li>'s own id/data-id only hold the digits BEFORE the account's trailing "-N"
    // check digit (e.g. "0335152071", not "0335152071-7") — an id-contains-accountNumber check can
    // never match because of that missing suffix. The .acc-name span's title/text does hold the
    // complete account number, so matching is done against that instead.
    //
    // This searches the whole document rather than scoping through the input's aria-controls id —
    // a real run showed the row fully rendered and visible in the dropdown while aria-controls never
    // got set on the input (the synthetic value+input-event trigger apparently doesn't reproduce every
    // side effect of real typing), so relying on that attribute silently never finds anything.
    //
    // A real run also showed window.frames.length === 2 on this page while WebView2's own
    // FrameCreated-based tracking (used to run script in a specific child frame) never actually
    // captured either of them — and separately turned out fragile (a stale frame from the previous
    // page briefly lingered and returned a non-JSON result, crashing a diagnostic). Recursing into
    // same-origin iframes directly via contentDocument, all within one script call in the main frame,
    // sidesteps that machinery entirely; a cross-origin iframe would throw on contentDocument access,
    // which is caught and skipped. This first attempt at that recursion still came back empty, though
    // — because it only crossed iframe boundaries and not shadow-DOM ones, and the rest of this file's
    // shadow-piercing helpers (AccountSearchBoxScript, FindInShadowJs) prove this Salesforce Lightning
    // site nests plenty of real shadow roots too. The search below crosses both at every level.
    private const string FindAccountRowJs = @"
function findAccountRow(accountNumber) {
  function search(root) {
    const nameSpan = Array.from(root.querySelectorAll('.acc-name')).find(s => (s.getAttribute('title') || s.textContent || '').trim() === accountNumber);
    if (nameSpan) return nameSpan.closest('[role=""option""]') || nameSpan.parentElement;

    for (const el of root.querySelectorAll('*')) {
      if (el.shadowRoot) {
        const found = search(el.shadowRoot);
        if (found) return found;
      }
    }

    for (const iframe of root.querySelectorAll('iframe')) {
      try {
        const innerDoc = iframe.contentDocument;
        if (innerDoc) {
          const found = search(innerDoc);
          if (found) return found;
        }
      } catch (e) { /* cross-origin iframe — inaccessible, skip it */ }
    }

    return null;
  }
  return search(document);
}
";

    private async Task<bool> SwitchToAccountAsync(WebView2 browser, string accountNumber)
    {
        var setValueScript = AccountSearchBoxScript + $@"
const el = findAccountBox(document);
if (!el) return false;
const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
setter.call(el, {JsonSerializer.Serialize(accountNumber)});
el.dispatchEvent(new Event('input', {{ bubbles: true }}));
return true;
";
        var found = await PgeWebViewAutomationHelper.ExecuteJsAsync(browser, setValueScript);
        if (found.Trim('"') != "true")
        {
            _logFile.Append($"SwitchToAccountAsync: could not find the account search box while switching to {accountNumber}.");
            return false;
        }

        // The dropdown's listbox is only attached to the DOM once the framework finishes its (async)
        // account search — a single fixed delay before looking for it caused intermittent failures
        // when that search happened to take longer, so this polls for the matching row to actually
        // exist instead of guessing a fixed wait.
        var findRowScript = FindAccountRowJs + $@"
const row = findAccountRow({JsonSerializer.Serialize(accountNumber)});
return row ? 'found' : 'not-found';
";

        var pollResult = "not-found";
        const int maxAttempts = 16;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            pollResult = (await PgeWebViewAutomationHelper.ExecuteJsAsync(browser, findRowScript)).Trim('"');
            if (pollResult == "found")
                break;

            await Task.Delay(500);
        }

        if (pollResult != "found")
        {
            _logFile.Append($"SwitchToAccountAsync: the account row for {accountNumber} never appeared (including inside same-origin iframes) after {maxAttempts * 500}ms — treating this as \"account not found\".");
            return false;
        }

        var clickScript = PgeWebViewAutomationHelper.HighlightAndClickJs + FindAccountRowJs + $@"
const row = findAccountRow({JsonSerializer.Serialize(accountNumber)});
if (!row) return 'no-match';
return highlightAndClick(row).then(() => 'clicked:' + (row.id || ''));
";
        var clickResult = (await PgeWebViewAutomationHelper.ExecuteJsAsync(browser, clickScript)).Trim('"');
        if (clickResult == "no-match")
        {
            _logFile.Append($"SwitchToAccountAsync: no-match — failed to click the account row for {accountNumber} even though it was found moments earlier.");
            return false;
        }

        // A result other than the expected "clicked:<id>" string (e.g. "{}") isn't necessarily a
        // failure — a real run showed this happening right after a successful click, because the
        // click itself kicked off a page transition that tore down the script's execution context
        // before its .then() could finish reporting back. Only the explicit "no-match" above (the
        // row genuinely wasn't there to click) is treated as a real failure.
        _logFile.Append($"SwitchToAccountAsync: click script returned '{clickResult}' for account {accountNumber} (treated as success unless it was 'no-match').");

        // Give the page a moment to actually reload the billing history for the newly-selected
        // account before the caller starts collecting rows from it.
        await Task.Delay(2500);
        return true;
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
        var pageCount = await PgeWebViewAutomationHelper.GetBillingHistoryPageCountAsync(browser);
        _logFile.Append($"CollectBillPdfLinksAsync: pager reports {pageCount} page(s) of billing history.");

        var all = new List<(string PdfUrl, string RowLabel)>();
        var seenRowLabels = new HashSet<string>();

        for (var page = 1; page <= pageCount; page++)
        {
            if (page > 1)
            {
                var moved = await PgeWebViewAutomationHelper.GoToBillingHistoryPageAsync(browser, page);
                _logFile.Append($"CollectBillPdfLinksAsync: navigating to page {page} of {pageCount} — {(moved ? "pager control found" : "pager control NOT found")}.");
                if (!moved)
                    break;

                await Task.Delay(1500);
            }

            var items = await CollectCurrentPageBillPdfLinksAsync(browser);
            _logFile.Append($"CollectBillPdfLinksAsync: page {page} of {pageCount} — {items.Count} 'View Bill PDF' row(s) found: {string.Join(" | ", items.Select(i => i.RowLabel))}");

            var newOnThisPage = 0;
            foreach (var item in items)
            {
                if (seenRowLabels.Add(item.RowLabel))
                {
                    all.Add(item);
                    newOnThisPage++;
                }
            }

            // If a page produced zero rows we haven't already seen, either the pager didn't actually
            // move (same page re-scraped) or we've genuinely run past the real content — either way,
            // continuing to "advance" further pages would just keep re-scraping the same data.
            if (newOnThisPage == 0 && page > 1)
            {
                _logFile.Append($"CollectBillPdfLinksAsync: page {page} produced no new rows, stopping pagination early.");
                break;
            }
        }

        return all;
    }

    private async Task<List<(string PdfUrl, string RowLabel)>> CollectCurrentPageBillPdfLinksAsync(WebView2 browser)
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

        _logFile.Append("CollectCurrentPageBillPdfLinksAsync: no 'View Bill PDF' rows found after 15 attempts.");
        return [];
    }

    private sealed class PdfLinkRow
    {
        public string href { get; set; } = "";
        public string label { get; set; } = "";
    }
}
