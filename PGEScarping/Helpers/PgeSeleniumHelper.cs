using System.Net;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace PGEScarping.Helpers;

// Selectors below are built from the portal screenshots supplied for this project, not a live DOM
// inspection (the portal sits behind login). Expect to recalibrate a selector or two on the first
// real run and adjust the XPath/CSS strings here accordingly.
public static class PgeSeleniumHelper
{
    public static IWebDriver CreateDriver(bool headless)
    {
        var options = new ChromeOptions();
        options.AddArgument("--start-maximized");
        options.AddArgument("--disable-blink-features=AutomationControlled");
        if (headless)
            options.AddArgument("--headless=new");

        var driver = new ChromeDriver(options);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
        return driver;
    }

    public static void Login(IWebDriver driver, string url, string username, string password, int timeoutSeconds)
    {
        driver.Navigate().GoToUrl(url);
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));

        var usernameField = wait.Until(d => d.FindElement(By.CssSelector(
            "input[type='email'], input[name*='username' i], input[id*='username' i]")));
        usernameField.Clear();
        usernameField.SendKeys(username);

        var passwordField = driver.FindElement(By.CssSelector("input[type='password']"));
        passwordField.Clear();
        passwordField.SendKeys(password);

        var loginButton = driver.FindElement(By.XPath(
            "//button[contains(translate(text(),'LOGIN','login'),'log in') or contains(translate(text(),'SIGNIN','signin'),'sign in')] | //input[@type='submit']"));
        loginButton.Click();

        wait.Until(d => d.FindElements(By.XPath("//*[contains(text(),'My Account Dashboard')]")).Count > 0);
    }

    public static List<string> DiscoverAccountNumbers(IWebDriver driver, int timeoutSeconds)
    {
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
        var searchBox = wait.Until(d => d.FindElements(By.XPath(
            "//h2[contains(text(),'Account')]/following::input[1]")).FirstOrDefault());

        var accounts = new List<string>();
        var currentAccountValue = searchBox?.GetAttribute("value");
        if (!string.IsNullOrWhiteSpace(currentAccountValue))
            accounts.Add(currentAccountValue.Trim());

        var switcherOptions = driver.FindElements(By.XPath("//ul[contains(@class,'account')]//li | //select[contains(@id,'account') or contains(@name,'account')]/option"));
        foreach (var option in switcherOptions)
        {
            var text = option.Text.Trim();
            if (!string.IsNullOrWhiteSpace(text) && !accounts.Contains(text))
                accounts.Add(text);
        }

        return accounts;
    }

    public static void SwitchToAccount(IWebDriver driver, string accountNumber, int timeoutSeconds)
    {
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
        var searchBox = wait.Until(d => d.FindElement(By.XPath("//h2[contains(text(),'Account')]/following::input[1]")));
        searchBox.Clear();
        searchBox.SendKeys(accountNumber);
        searchBox.SendKeys(OpenQA.Selenium.Keys.Enter);
        Thread.Sleep(1500);
    }

    public static List<(string PdfUrl, string RowLabel)> CollectBillPdfLinks(IWebDriver driver, string billHistoryUrl, int timeoutSeconds)
    {
        driver.Navigate().GoToUrl(billHistoryUrl);
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
        wait.Until(d => d.FindElements(By.XPath("//table")).Count > 0);

        var results = new List<(string, string)>();
        var totalPages = GetTotalPages(driver);

        for (var page = 1; page <= totalPages; page++)
        {
            if (page > 1)
                JumpToPage(driver, page, wait);

            var rows = driver.FindElements(By.XPath("//tr[.//a[contains(text(),'View Bill PDF')]]"));
            foreach (var row in rows)
            {
                var link = row.FindElement(By.XPath(".//a[contains(text(),'View Bill PDF')]"));
                var href = link.GetAttribute("href");
                var rowLabel = row.Text.Replace("\n", " ").Trim();
                if (!string.IsNullOrWhiteSpace(href))
                    results.Add((href, rowLabel));
            }
        }

        return results;
    }

    private static int GetTotalPages(IWebDriver driver)
    {
        var pagerText = driver.FindElements(By.XPath("//*[contains(text(),'Jump to')]")).FirstOrDefault()?.Text ?? "";
        var match = System.Text.RegularExpressions.Regex.Match(pagerText, @"of\s+(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 1;
    }

    private static void JumpToPage(IWebDriver driver, int page, WebDriverWait wait)
    {
        var dropdown = driver.FindElements(By.XPath("//select[.//option[contains(@value,'1')]]")).FirstOrDefault();
        if (dropdown is null)
            return;

        var select = new SelectElement(dropdown);
        select.SelectByValue(page.ToString());
        Thread.Sleep(1200);
    }

    public static byte[] DownloadPdfWithSessionCookies(IWebDriver driver, string pdfUrl)
    {
        var cookieContainer = new CookieContainer();
        foreach (var cookie in driver.Manage().Cookies.AllCookies)
        {
            try
            {
                cookieContainer.Add(new System.Net.Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain?.TrimStart('.') ?? ""));
            }
            catch (CookieException)
            {
                // Some Selenium cookie domains are stricter than System.Net.Cookie accepts; skip and continue.
            }
        }

        using var handler = new HttpClientHandler { CookieContainer = cookieContainer };
        using var httpClient = new HttpClient(handler);
        return httpClient.GetByteArrayAsync(pdfUrl).GetAwaiter().GetResult();
    }
}
