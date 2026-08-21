using System.Globalization;
using System.Text.RegularExpressions;
using PGEScarping.Models;
using UglyToad.PdfPig;

namespace PGEScarping.Helpers;

public static class PdfBillParserHelper
{
    public static PgeBillRecord Parse(byte[] pdfBytes, string billPdfFileName)
    {
        var text = ExtractText(pdfBytes);

        var record = new PgeBillRecord
        {
            BillPdfFileName = billPdfFileName,
            AccountNumber = MatchText(text, @"Account No:?\s*([\d\-]+)"),
            StatementDate = MatchDate(text, @"Statement Date:?\s*(\d{2}/\d{2}/\d{4})"),
            DueDate = MatchDate(text, @"Due Date:?\s*(\d{2}/\d{2}/\d{4})"),
            TotalBillAmount = MatchAmount(text, @"Total Amount Due[^$]*\$\s*(-?[\d,]+\.\d{2})"),
            TotalUsageKwh = MatchAmount(text, @"Total Usage\s+([\d,]+\.\d+)\s*kWh")
        };

        // PG&E Electric Delivery Charges section
        var customerCharge = MatchAmount(text, @"Customer Charge\s+\d+\s+days\s+@\$[\d.]+\s+\$?(-?[\d,]+\.\d{2})");
        var peak = MatchAmount(text, @"(?<!Off )\bPeak\b(?!\s*Summer)\s+[\d.,]+\s*kWh\s*@\$[\d.]+\s+(-?[\d,]+\.\d{2})");
        var offPeak = MatchAmount(text, @"Off Peak\s+[\d.,]+\s*kWh\s*@\$[\d.]+\s+(-?[\d,]+\.\d{2})");
        var generationCredit = MatchAmount(text, @"Generation Credit\s+(-?[\d,]+\.\d{2})");
        var pcia = MatchAmount(text, @"Power Charge Indifference Adjustment(?!\s+Credit)\s+(-?[\d,]+\.\d{2})");
        var franchiseFee = MatchAmount(text, @"Franchise Fee Surcharge(?!\s+Credit)\s+(-?[\d,]+\.\d{2})");
        var stocktonTax = MatchAmount(text, @"Stockton Utility Users.{0,2}Tax[^\d\-]*(-?[\d,]+\.\d{2})");

        // Ava Community Energy Generation Charges section (may not exist for PG&E-only accounts)
        var offPeakSummer = MatchAmount(text, @"Off-Peak Summer\s+[\d.,]+\s*kWh\s*@\$[\d.]+\s+\$?(-?[\d,]+\.\d{2})");
        var peakSummer = MatchAmount(text, @"Peak Summer\s+[\d.,]+\s*kWh\s*@\$[\d.]+\s+(-?[\d,]+\.\d{2})");
        var pciaCredit = MatchAmount(text, @"Power Charge Indifference Adjustment Credit\s+(-?[\d,]+\.\d{2})");
        var franchiseFeeCredit = MatchAmount(text, @"Franchise Fee Surcharge Credit\s+(-?[\d,]+\.\d{2})");
        var brightChoice = MatchAmount(text, @"Bright Choice\s+(-?[\d,]+\.\d{2})");
        var localUtilityTax = MatchAmount(text, @"Local Utility Users Tax[^\d\-]*(-?[\d,]+\.\d{2})");
        var energyCommissionTax = MatchAmount(text, @"Energy Commission Tax\s+(-?[\d,]+\.\d{2})");

        record.ElectricityCharges = peak + offPeak + peakSummer + offPeakSummer;
        record.CreditReceived = Math.Abs(generationCredit) + Math.Abs(pciaCredit) + Math.Abs(franchiseFeeCredit);
        record.TotalTaxAmount = stocktonTax + localUtilityTax + energyCommissionTax;
        record.OtherCharges = customerCharge + pcia + franchiseFee + brightChoice;

        return record;
    }

    private static string ExtractText(byte[] pdfBytes)
    {
        using var stream = new MemoryStream(pdfBytes);
        using var document = PdfDocument.Open(stream);

        var pages = document.GetPages().Select(page => page.Text);
        return string.Join("\n", pages);
    }

    private static string MatchText(string text, string pattern)
    {
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : "";
    }

    private static DateTime? MatchDate(string text, string pattern)
    {
        var value = MatchText(text, pattern);
        return DateTime.TryParseExact(value, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    private static decimal MatchAmount(string text, string pattern)
    {
        var value = MatchText(text, pattern).Replace(",", "").Replace("$", "");
        return decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var amount)
            ? amount
            : 0m;
    }
}
