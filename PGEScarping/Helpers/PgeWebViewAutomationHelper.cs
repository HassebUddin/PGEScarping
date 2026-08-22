using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace PGEScarping.Helpers;

// The PG&E portal is a Salesforce Experience Cloud (Lightning) site, so most interactive elements
// live inside nested shadow DOM. Every interaction below pierces shadow roots via JS rather than
// relying on plain CSS selectors, which won't reach into shadow-hosted components.
public static class PgeWebViewAutomationHelper
{
    // Flashes a bright red glow around an element before clicking it — since the embedded browser is
    // always visible in the app's own window, this lets the user actually see, in real time, exactly
    // which element the automation is about to click, instead of it happening invisibly fast.
    // ExecuteScriptAsync awaits any Promise a script returns, so returning one here makes the C# await
    // naturally wait out the highlight-then-click sequence with no extra round trip needed.
    public const string HighlightAndClickJs = @"
function highlightAndClick(el) {
  return new Promise(resolve => {
    el.scrollIntoView({ block: 'center', inline: 'center' });
    const prevOutline = el.style.outline;
    const prevBoxShadow = el.style.boxShadow;
    el.style.outline = '3px solid #ff3366';
    el.style.boxShadow = '0 0 14px 5px rgba(255,51,102,0.85)';
    setTimeout(() => {
      el.click();
      setTimeout(() => {
        el.style.outline = prevOutline;
        el.style.boxShadow = prevBoxShadow;
      }, 400);
      resolve(true);
    }, 700);
  });
}
";

    public const string FindInShadowJs = @"
function findInShadow(root, selector) {
  const direct = root.querySelector(selector);
  if (direct) return direct;
  for (const el of root.querySelectorAll('*')) {
    if (el.shadowRoot) {
      const found = findInShadow(el.shadowRoot, selector);
      if (found) return found;
    }
  }
  return null;
}
function findByTextInShadow(root, tag, text) {
  for (const el of root.querySelectorAll(tag)) {
    if ((el.innerText || '').trim().toLowerCase() === text) return el;
  }
  for (const el of root.querySelectorAll('*')) {
    if (el.shadowRoot) {
      const found = findByTextInShadow(el.shadowRoot, tag, text);
      if (found) return found;
    }
  }
  return null;
}
";

    public static async Task NavigateAndWaitAsync(WebView2 browser, string url, int renderDelayMs = 5000)
    {
        var tcs = new TaskCompletionSource();
        void Handler(object? s, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e) => tcs.TrySetResult();
        browser.CoreWebView2.NavigationCompleted += Handler;
        try
        {
            browser.CoreWebView2.Navigate(url);
            await tcs.Task;
        }
        finally
        {
            browser.CoreWebView2.NavigationCompleted -= Handler;
        }

        await Task.Delay(renderDelayMs);
    }

    public static async Task<bool> ClickInShadowAsync(WebView2 browser, string selector)
    {
        var script = FindInShadowJs + $@"
const el = findInShadow(document, {JsonSerializer.Serialize(selector)});
if (el) {{ el.click(); return true; }}
return false;
";
        var result = await browser.CoreWebView2.ExecuteScriptAsync(WrapAsFunction(script));
        return result.Trim('"') == "true";
    }

    public static async Task<bool> ClickButtonByTextInShadowAsync(WebView2 browser, string text)
    {
        var script = FindInShadowJs + $@"
const el = findByTextInShadow(document, 'button', {JsonSerializer.Serialize(text.ToLowerInvariant())});
if (el) {{ el.click(); return true; }}
return false;
";
        var result = await browser.CoreWebView2.ExecuteScriptAsync(WrapAsFunction(script));
        return result.Trim('"') == "true";
    }

    public static async Task<bool> SetShadowInputValueAsync(WebView2 browser, string selector, string value)
    {
        var script = FindInShadowJs + $@"
const el = findInShadow(document, {JsonSerializer.Serialize(selector)});
if (!el) return false;
const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
setter.call(el, {JsonSerializer.Serialize(value)});
el.dispatchEvent(new Event('input', {{ bubbles: true }}));
el.dispatchEvent(new Event('change', {{ bubbles: true }}));
return true;
";
        var result = await browser.CoreWebView2.ExecuteScriptAsync(WrapAsFunction(script));
        return result.Trim('"') == "true";
    }

    public static async Task<bool> ElementExistsInShadowAsync(WebView2 browser, string selector)
    {
        var script = FindInShadowJs + $@"
return !!findInShadow(document, {JsonSerializer.Serialize(selector)});
";
        var result = await browser.CoreWebView2.ExecuteScriptAsync(WrapAsFunction(script));
        return result.Trim('"') == "true";
    }

    public static async Task<string?> GetShadowInputValueAsync(WebView2 browser, string selector)
    {
        var script = FindInShadowJs + $@"
const el = findInShadow(document, {JsonSerializer.Serialize(selector)});
return el ? el.value : null;
";
        var result = await browser.CoreWebView2.ExecuteScriptAsync(WrapAsFunction(script));
        return JsonSerializer.Deserialize<string?>(result);
    }

    public static async Task<bool> WaitForShadowElementAsync(WebView2 browser, string selector, int timeoutMs = 20000, int pollIntervalMs = 800)
    {
        var elapsed = 0;
        while (elapsed < timeoutMs)
        {
            if (await ElementExistsInShadowAsync(browser, selector))
                return true;

            await Task.Delay(pollIntervalMs);
            elapsed += pollIntervalMs;
        }

        return false;
    }

    // The billing history table's "Jump to [N] of <total>" pager is a Lightning base combobox
    // (confirmed via live devtools inspection): a
    //   <button role=""combobox"" aria-haspopup=""listbox"" aria-controls=""dropdown-element-NNN"" data-value=""1"">
    // whose listbox (found directly via that aria-controls id — ids are unique document-wide, so this
    // works regardless of any shadow-DOM boundaries in between) contains one
    //   <lightning-base-combobox-item role=""option"" data-value=""N"">
    // per page. This is distinguished from unrelated comboboxes on the page (like the "Filter: All
    // Activity" one, or a possible "rows per page" selector with values like 10/25/50) by checking
    // that its option values are the exact sequence 1, 2, 3, ... N — the one shape a page-jump
    // control always has and nothing else on the page plausibly would.
    private const string FindPagerButtonJs = @"
function findPagerButton(root) {
  for (const btn of root.querySelectorAll('button[role=""combobox""][aria-haspopup=""listbox""]')) {
    const listboxId = btn.getAttribute('aria-controls');
    const listbox = listboxId ? document.getElementById(listboxId) : null;
    if (!listbox) continue;
    const values = Array.from(listbox.querySelectorAll('[role=""option""]')).map(o => parseInt((o.getAttribute('data-value') || '').trim(), 10));
    if (values.length > 1 && values.every((v, i) => v === i + 1)) return btn;
  }
  for (const el of root.querySelectorAll('*')) {
    if (el.shadowRoot) {
      const found = findPagerButton(el.shadowRoot);
      if (found) return found;
    }
  }
  return null;
}
";

    public static async Task<int> GetBillingHistoryPageCountAsync(WebView2 browser)
    {
        var script = FindPagerButtonJs + @"
const btn = findPagerButton(document);
if (!btn) return 1;
const listbox = document.getElementById(btn.getAttribute('aria-controls'));
return listbox ? listbox.querySelectorAll('[role=""option""]').length : 1;
";
        var result = await browser.CoreWebView2.ExecuteScriptAsync(WrapAsFunction(script));
        return int.TryParse(result, out var count) ? count : 1;
    }

    public static async Task<bool> GoToBillingHistoryPageAsync(WebView2 browser, int pageNumber)
    {
        var openScript = HighlightAndClickJs + FindPagerButtonJs + @"
const btn = findPagerButton(document);
if (!btn) return false;
btn.focus();
return highlightAndClick(btn).then(() => true);
";
        var opened = (await browser.CoreWebView2.ExecuteScriptAsync(WrapAsFunction(openScript))).Trim('"');
        if (opened != "true")
            return false;

        await Task.Delay(400);

        var pickScript = HighlightAndClickJs + FindPagerButtonJs + $@"
const btn = findPagerButton(document);
if (!btn) return false;
const listbox = document.getElementById(btn.getAttribute('aria-controls'));
if (!listbox) return false;
const opt = Array.from(listbox.querySelectorAll('[role=""option""]')).find(o => (o.getAttribute('data-value') || '').trim() === {JsonSerializer.Serialize(pageNumber.ToString())});
if (!opt) return false;
return highlightAndClick(opt).then(() => true);
";
        var result = await browser.CoreWebView2.ExecuteScriptAsync(WrapAsFunction(pickScript));
        return result.Trim('"') == "true";
    }

    // Clicking "View Bill PDF" always opens a real new tab (confirmed against the live site) — a
    // suppressed NewWindowRequested that just re-navigates the main frame to the reported URL was not
    // enough to actually load the PDF, which points at this being a case where the target genuinely
    // needs to be opened as its own top-level browsing context. WebView2's documented way to honor
    // window.open() faithfully is to create a real second CoreWebView2 for the popup and hand it to
    // NewWindowRequested via e.NewWindow.
    //
    // The popup ends up on a blob: URL — PG&E generates the PDF client-side and points the tab at an
    // in-memory Blob rather than a real network response, which is why nothing ever showed up over
    // CDP's Network/Fetch domains no matter how the request was made, and fetching the blob: URL back
    // out after navigating to it fails too ("Failed to fetch"), because Chromium's built-in PDF viewer
    // takes the blob into its own restricted rendering context. So instead of reacting after the fact,
    // this patches URL.createObjectURL to capture the PDF Blob at the moment it's created — while
    // it's still a completely normal, readable JS object, before the restricted viewer ever gets it.
    //
    // Comparing an automated run's log against a manual click revealed the blob is actually created in
    // the *opener* document (the billing-history page in the main window), not inside the popup —
    // window.open(URL.createObjectURL(blob)) is a common pattern, and the created URL is just a string
    // handed to the new tab, so the patch has to be live in the main window *before* the click happens,
    // not only in the popup.
    private const string CaptureBlobJs = @"
(function() {
  if (window.__pgePdfCapturePatched) return;
  window.__pgePdfCapturePatched = true;
  const original = URL.createObjectURL.bind(URL);
  URL.createObjectURL = function(blob) {
    try {
      if (blob && typeof blob.arrayBuffer === 'function' && (blob.type || '').toLowerCase().includes('pdf')) {
        blob.arrayBuffer().then(function(buffer) {
          const bytes = new Uint8Array(buffer);
          let binary = '';
          const chunkSize = 8192;
          for (let i = 0; i < bytes.length; i += chunkSize) {
            binary += String.fromCharCode.apply(null, bytes.subarray(i, i + chunkSize));
          }
          window.chrome.webview.postMessage(JSON.stringify({ ok: true, data: btoa(binary) }));
        }).catch(function(e) {
          window.chrome.webview.postMessage(JSON.stringify({ ok: false, error: String(e) }));
        });
      }
    } catch (e) { /* fall through to the real createObjectURL regardless */ }
    return original(blob);
  };
})();
";

    public static async Task<byte[]> ClickBillPdfLinkAndDownloadAsync(WebView2 browser, string rowLabel, AppLogFile? logFile = null)
    {
        var core = browser.CoreWebView2;
        var pdfTcs = new TaskCompletionSource<byte[]>();
        CoreWebView2Controller? popupController = null;

        void OnMessage(object? s, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (pdfTcs.Task.IsCompleted)
                return;

            try
            {
                var payload = JsonSerializer.Deserialize<string>(e.WebMessageAsJson) ?? e.WebMessageAsJson;
                using var result = JsonDocument.Parse(payload);
                if (result.RootElement.TryGetProperty("ok", out var okProp) && okProp.GetBoolean())
                    pdfTcs.TrySetResult(Convert.FromBase64String(result.RootElement.GetProperty("data").GetString()!));
                else
                    pdfTcs.TrySetException(new InvalidOperationException($"Blob capture failed: {payload}"));
            }
            catch (Exception ex)
            {
                pdfTcs.TrySetException(ex);
            }
        }

        async void OnNewWindow(object? s, CoreWebView2NewWindowRequestedEventArgs e)
        {
            var deferral = e.GetDeferral();
            try
            {
                // Left visible (rather than a tiny hidden window) so what the popup is doing —
                // generating the PDF, or stuck/erroring — can actually be watched.
                popupController = await core.Environment.CreateCoreWebView2ControllerAsync(browser.Handle);
                popupController.Bounds = new System.Drawing.Rectangle(80, 80, 1000, 750);
                popupController.IsVisible = true;

                var popupCore = popupController.CoreWebView2;
                popupCore.NavigationStarting += (_, navArgs) => logFile?.Append($"Popup navigating to: {navArgs.Uri}");
                popupCore.NavigationCompleted += (_, _) => logFile?.Append($"Popup navigation completed: {popupCore.Source}");
                await popupCore.AddScriptToExecuteOnDocumentCreatedAsync(CaptureBlobJs);
                popupCore.WebMessageReceived += OnMessage;

                e.NewWindow = popupCore;
                e.Handled = true;
            }
            catch (Exception ex)
            {
                pdfTcs.TrySetException(ex);
            }
            finally
            {
                deferral.Complete();
            }
        }

        // Patch the *current* (already-loaded) billing-history page too — this is where the blob is
        // actually created in practice, per the working-vs-not-working comparison above.
        core.WebMessageReceived += OnMessage;
        await ExecuteJsAsync(browser, CaptureBlobJs);
        core.NewWindowRequested += OnNewWindow;
        try
        {
            var script = FindInShadowJs + $@"
function findRowLink(root) {{
  for (const row of root.querySelectorAll('tr')) {{
    const text = row.innerText.replace(/\n/g, ' ').trim();
    if (text === {JsonSerializer.Serialize(rowLabel)}) {{
      const link = Array.from(row.querySelectorAll('a')).find(a => (a.innerText || '').includes('View Bill PDF'));
      if (link) return link;
    }}
  }}
  for (const el of root.querySelectorAll('*')) {{
    if (el.shadowRoot) {{
      const found = findRowLink(el.shadowRoot);
      if (found) return found;
    }}
  }}
  return null;
}}
const el = findRowLink(document);
if (el) {{ el.click(); return true; }}
return false;
";
            await ExecuteJsAsync(browser, script);

            // PG&E generates the PDF on demand ("Your document is almost ready...") before the popup's
            // final navigation actually returns it, so this can take noticeably longer than a normal
            // page load — confirmed against the live site.
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            await using (cts.Token.Register(() => pdfTcs.TrySetException(new TimeoutException("Timed out waiting for the PDF response from the popup window."))))
            {
                var bytes = await pdfTcs.Task;
                logFile?.Append($"ClickBillPdfLinkAndDownloadAsync: got {bytes.Length} bytes.");
                return bytes;
            }
        }
        finally
        {
            core.NewWindowRequested -= OnNewWindow;
            core.WebMessageReceived -= OnMessage;
            try { popupController?.Close(); } catch { /* best effort cleanup */ }
        }
    }

    public static async Task<string> ExecuteJsAsync(WebView2 browser, string script)
        => await browser.CoreWebView2.ExecuteScriptAsync(WrapAsFunction(script));

    private static string WrapAsFunction(string body) => $"(function() {{ {body} }})()";
}
