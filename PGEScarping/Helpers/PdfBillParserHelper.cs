using System.Globalization;
using System.Text.RegularExpressions;
using PGEScarping.Models;
using UglyToad.PdfPig;

namespace PGEScarping.Helpers;

public static class PdfBillParserHelper
{
    public static PgeBillRecord Parse(byte[] pdfBytes, string billPdfFileName, AppLogFile? logFile = null)
    {
        var text = ExtractText(pdfBytes);
        logFile?.Append($"PdfBillParserHelper: extracted text for {billPdfFileName}:{Environment.NewLine}{text}");

        var record = new PgeBillRecord
        {
            BillPdfFileName = billPdfFileName,
            AccountNumber = MatchText(text, @"Account No:?\s*([\d\-]+)"),
            StatementDate = MatchDate(text, @"Statement Date:?\s*(\d{2}/\d{2}/\d{4})"),
            DueDate = MatchDate(text, @"Due Date:?\s*(\d{2}/\d{2}/\d{4})"),
            TotalBillAmount = MatchAmount(text, @"Total Amount Due[^$]*\$\s*(-?[\d,]+\.\d{2})"),
            TotalUsageKwh = MatchAmount(text, @"Total Usage\s+([\d,]+\.\d+)\s*kWh"),
            // Only some accounts have gas service — "Total Gas Charges" simply won't be present on an
            // electric-only bill, and MatchAmount already returns 0 when a pattern doesn't match.
            GasCharges = MatchAmount(text, @"Total Gas Charges\s+\$?(-?[\d,]+\.\d{2})")
        };

        // Electricity usage charges: PG&E ("Peak"/"Off Peak") and various CCAs ("Peak Summer"/
        // "Off-Peak Winter", or CleanPowerSF's "Generation - On Peak - Summer" with dashes around both
        // the season word and — for "On Peak"/"Part Peak" — around "Peak" itself) all follow the same
        // "<label> <kWh> @$<rate> <amount>" shape. The match only needs to find the bare "Peak"/"Off
        // Peak" substring — any "On "/"Part "/"Generation - " prefix before it is simply left
        // unconsumed — so the optional season suffix is the only part that needs to tolerate the
        // dashes CleanPowerSF wraps it in ("- Summer" instead of just " Summer"). A bill can also list
        // more than one billing period for the same tier (e.g. a mid-cycle rate change), so every
        // matching line is summed rather than just the first.
        // Note: PdfPig's word-level extraction can split "@" and "$<rate>" into separate words with a
        // space between them (positioning-dependent), so "@\s*\$?" is used everywhere instead of a
        // literal "@$" — a real PDF sample confirmed a bare "@\$" match fails and silently zeroes out
        // every downstream sum.
        record.ElectricityCharges = SumAllMatches(text,
            @"(?:Off[- ]?Peak(?:\s*-?\s*(?:Summer|Winter))?|Peak(?:\s*-?\s*(?:Summer|Winter))?)\s+[\d.,]+\s*kWh\s*@\s*\$?[\d.]+\s*\$?(-?[\d,]+\.\d{2})");

        // Credits: PG&E's Generation Credit and Baseline Credit, plus Ava/MCE's PCIA Credit, Franchise
        // Fee Surcharge Credit, Bright Choice, and MCE Cost Relief Credit. Baseline Credit and MCE Cost
        // Relief Credit are per-kWh lines like the Electricity Charges ones (e.g. "Baseline Credit
        // 465.000000 kWh @-$0.08140 -37.85" — note the rate itself can carry its own "-"), so they need
        // the same optional "<kWh> kWh @$<rate>" clause before the real dollar amount, otherwise the
        // kWh figure itself would get captured instead of the trailing credit amount.
        // The bill itself shows these as negative amounts (e.g. "Generation Credit -357.25"), and the
        // Excel output keeps that same negative sign rather than flipping it positive.
        record.CreditReceived = SumAllMatches(text, @"Generation Credit\s+(-?[\d,]+\.\d{2})")
            + SumAllMatches(text, @"Baseline Credit(?:\s+[\d.,]+\s*kWh\s*@\s*-?\$?-?[\d.]+)?\s+\$?(-?[\d,]+\.\d{2})")
            + SumAllMatches(text, @"Power Charge Indifference Adjustment Credit\s+(-?[\d,]+\.\d{2})")
            + SumAllMatches(text, @"Franchise Fee Surcharge Credit\s+(-?[\d,]+\.\d{2})")
            + SumAllMatches(text, @"Bright Choice\s+(-?[\d,]+\.\d{2})")
            + SumAllMatches(text, @"MCE Cost Relief Credit(?:\s+[\d.,]+\s*kWh\s*@\s*\$?[\d.]+)?\s+\$?(-?[\d,]+\.\d{2})");

        // Taxes: PG&E's utility users' tax (name varies by city) plus Ava's local utility tax and
        // energy commission tax. Each tax line is followed by a "(6.000%)" rate parenthetical before
        // the actual dollar amount — that parenthetical must be consumed explicitly, otherwise
        // "[^\d\-]*" stops at the first digit inside "(6.000%)" and the decimal-amount pattern matches
        // a truncated fragment of the percentage itself instead of the real amount after it.
        // The Gas Charges section re-lists a "Stockton Utility Users' Tax (6.000%)" line per billing
        // tier for the gas usage, with the exact same label text as the electric one — scoping the
        // search to the text before "Details of Gas Charges" keeps this an electric-only total,
        // consistent with ElectricityCharges/OtherCharges only ever matching electric line items.
        var gasSectionIndex = text.IndexOf("Details of Gas Charges", StringComparison.OrdinalIgnoreCase);
        var electricPortion = gasSectionIndex >= 0 ? text[..gasSectionIndex] : text;

        // The city name in "<City> Utility Users' Tax" varies per account (Stockton, Fairfield,
        // San Francisco, etc.) — matched generically instead of hardcoding one city, so any account's
        // city is picked up automatically. "Local" is excluded from the city-name slot so this can
        // never re-match (and double-count) the separate "Local Utility Users Tax" line matched right
        // below it. "Energy Commission Tax" and "Energy Commission Surcharge" are the same line item
        // worded differently by different CCAs, matched as one alternation. A city can also levy its
        // own separate tax surcharge (e.g. San Francisco's "SF Prop C Tax Surcharge") on top of its
        // Utility Users' Tax — that's matched by its literal "Tax Surcharge" ending, a phrase specific
        // enough not to collide with any of the other tax patterns here.
        record.TotalTaxAmount = SumAllMatches(electricPortion, @"\b(?!Local\b)[A-Za-z]+\s+Utility Users.{0,2}Tax(?:\s*\(\s*[\d.]+\s*%\s*\))?\s+\$?(-?[\d,]+\.\d{2})")
            + SumAllMatches(electricPortion, @"Local Utility Users Tax(?:\s*\(\s*[\d.]+\s*%\s*\))?\s+\$?(-?[\d,]+\.\d{2})")
            + SumAllMatches(electricPortion, @"Energy Commission (?:Tax|Surcharge)\s+(-?[\d,]+\.\d{2})")
            + SumAllMatches(electricPortion, @"[A-Za-z][A-Za-z'\-\s]*?Tax Surcharge\s+(-?[\d,]+\.\d{2})");

        // Everything else: the flat per-period service charge (renamed from "Customer Charge" to
        // "Base Services Charge" starting March 2026 — both forms are matched) plus PG&E's non-credit
        // Power Charge Indifference Adjustment and Franchise Fee Surcharge.
        // Scoped to electricPortion (same as TotalTaxAmount) rather than the full text — the Gas
        // Charges section has its own "Customer Charge <N> days @$<rate> $<amount>" line in the exact
        // same shape as the electric one, and that gas amount is already captured separately in
        // GasCharges; without this scoping it would get double-counted into OtherCharges too.
        record.OtherCharges = SumAllMatches(electricPortion, @"(?:Customer Charge|Base Services Charge)\s+\d+\s*days\s*@\s*\$?[\d.]+\s*\$?(-?[\d,]+\.\d{2})")
            + SumAllMatches(electricPortion, @"Power Charge Indifference Adjustment(?!\s+Credit)\s+(-?[\d,]+\.\d{2})")
            + SumAllMatches(electricPortion, @"Franchise Fee Surcharge(?!\s+Credit)\s+(-?[\d,]+\.\d{2})");

        return record;
    }

    private static string ExtractText(byte[] pdfBytes)
    {
        using var stream = new MemoryStream(pdfBytes);
        using var document = PdfDocument.Open(stream);

        var sb = new System.Text.StringBuilder();
        foreach (var page in document.GetPages())
        {
            // PdfPig's word stream follows the PDF's drawing order, not visual reading order — on
            // these bills a line's dollar amount is frequently drawn before its label (confirmed
            // against a real sample: "$100.17 Peak" in the raw word stream vs. "Peak ... $100.17" on
            // the printed page), which silently breaks every "<label> ... <amount>" regex below.
            // Reconstructing lines from each word's Y-position (grouping words whose bottom edge is
            // within a couple of points of the previous word in the same top-to-bottom sweep, so
            // unrelated same-page columns like a sidebar QR code don't anchor a line's grouping) and
            // then sorting each line's words left-to-right by X restores real reading order.
            var sortedByBottom = page.GetWords().OrderByDescending(w => w.BoundingBox.Bottom).ToList();
            var lines = new List<List<UglyToad.PdfPig.Content.Word>>();
            foreach (var word in sortedByBottom)
            {
                var current = lines.Count > 0 ? lines[^1] : null;
                if (current != null && Math.Abs(current[^1].BoundingBox.Bottom - word.BoundingBox.Bottom) <= 2.5)
                    current.Add(word);
                else
                    lines.Add(new List<UglyToad.PdfPig.Content.Word> { word });
            }

            foreach (var line in lines)
            {
                foreach (var word in line.OrderBy(w => w.BoundingBox.Left))
                {
                    sb.Append(word.Text);
                    sb.Append(' ');
                }
                sb.Append('\n');
            }
        }

        return sb.ToString();
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

    private static decimal SumAllMatches(string text, string pattern)
    {
        decimal total = 0;
        foreach (Match match in Regex.Matches(text, pattern, RegexOptions.IgnoreCase))
        {
            var value = match.Groups[1].Value.Replace(",", "").Replace("$", "");
            if (decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var amount))
                total += amount;
        }

        return total;
    }
}
