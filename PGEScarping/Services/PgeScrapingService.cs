using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PGEScarping.Dto;
using PGEScarping.Enums;
using PGEScarping.Helpers;
using PGEScarping.Interfaces;
using PGEScarping.Models;
using PGEScarping.Options;

namespace PGEScarping.Services;

public sealed class PgeScrapingService : IPgeScrapingService
{
    private readonly IScrapingWebsiteRepository _scrapingWebsiteRepository;
    private readonly ScrapingOptions _options;
    private readonly ILogger<PgeScrapingService> _logger;

    public PgeScrapingService(
        IScrapingWebsiteRepository scrapingWebsiteRepository,
        IOptions<ScrapingOptions> options,
        ILogger<PgeScrapingService> logger)
    {
        _scrapingWebsiteRepository = scrapingWebsiteRepository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PgeScrapeResult> RunAsync(IProgress<string> progress, CancellationToken cancellationToken = default)
    {
        try
        {
            var website = await _scrapingWebsiteRepository.GetActiveBySourceTypeIdAsync((int)ScrapingSourceType.PGEBilling);
            if (website is null)
                return new PgeScrapeResult { Success = false, Message = "No active PG&E website credentials found in the database." };

            progress.Report($"Logging in to {website.website_url} as {website.username} ...");

            using var driver = PgeSeleniumHelper.CreateDriver(_options.Headless);
            PgeSeleniumHelper.Login(driver, website.website_url, website.username, website.password, _options.TimeoutSeconds);
            progress.Report("Login successful.");

            var accountNumbers = PgeSeleniumHelper.DiscoverAccountNumbers(driver, _options.TimeoutSeconds);
            if (accountNumbers.Count == 0)
                return new PgeScrapeResult { Success = false, Message = "No accounts found under this login." };

            progress.Report($"Found {accountNumbers.Count} account(s): {string.Join(", ", accountNumbers)}");

            var allBills = new List<PgeBillRecord>();

            foreach (var accountNumber in accountNumbers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress.Report($"Account {accountNumber}: switching...");
                PgeSeleniumHelper.SwitchToAccount(driver, accountNumber, _options.TimeoutSeconds);

                var billingHistoryUrl = new Uri(new Uri(website.website_url), "/myaccount/s/bill-and-payment-history").ToString();
                var pdfLinks = PgeSeleniumHelper.CollectBillPdfLinks(driver, billingHistoryUrl, _options.TimeoutSeconds);
                progress.Report($"Account {accountNumber}: found {pdfLinks.Count} bill(s) in history.");

                foreach (var (pdfUrl, rowLabel) in pdfLinks)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress.Report($"Account {accountNumber}: downloading bill ({rowLabel})...");

                    var pdfBytes = PgeSeleniumHelper.DownloadPdfWithSessionCookies(driver, pdfUrl);
                    var fileName = $"{accountNumber}_{allBills.Count + 1}.pdf";
                    var record = PdfBillParserHelper.Parse(pdfBytes, fileName);
                    record.AccountNumber = string.IsNullOrWhiteSpace(record.AccountNumber) ? accountNumber : record.AccountNumber;

                    allBills.Add(record);
                    progress.Report($"Account {accountNumber}: parsed bill dated {record.StatementDate:MM/dd/yyyy}, total ${record.TotalBillAmount}.");
                }
            }

            Directory.CreateDirectory(_options.OutputFolder);
            var outputFilePath = Path.Combine(_options.OutputFolder, "PGE_Billing_History.xlsx");
            ExcelExportHelper.WriteWorkbook(outputFilePath, allBills);

            progress.Report($"Done. {allBills.Count} bill(s) written to {outputFilePath}.");

            return new PgeScrapeResult
            {
                Success = true,
                Message = $"{allBills.Count} bill(s) exported across {accountNumbers.Count} account(s).",
                OutputFilePath = outputFilePath,
                Bills = allBills
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PgeScrapingService.RunAsync failed");
            progress.Report($"Error: {ex.Message}");
            return new PgeScrapeResult { Success = false, Message = ex.Message };
        }
    }
}
