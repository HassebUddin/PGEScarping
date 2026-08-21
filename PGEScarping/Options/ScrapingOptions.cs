namespace PGEScarping.Options;

public sealed class ScrapingOptions
{
    public const string SectionName = "Scraping";

    public bool Headless { get; set; } = false;
    public int TimeoutSeconds { get; set; } = 60;
    public string OutputFolder { get; set; } = @"C:\PGEBillingReports";
}
