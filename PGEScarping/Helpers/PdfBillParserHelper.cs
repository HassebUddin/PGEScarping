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

        // Every real line item (energy usage, credits, taxes, flat charges) lives inside the
        // "Details of <Provider> Electric Delivery/Generation Charges" ... "Total <Provider> Electric
        // Delivery/Generation Charges" blocks. A later, purely informational page ("Your Electric
        // Charges Breakdown") re-lists an unrelated rollup of the SAME total under different labels
        // (Transmission, Distribution, Recovery Bond Credit, Wildfire Fund Charge, PCIA, "Taxes and
        // Other", etc.) — several of which contain the words "Charge"/"Credit"/"Tax" too. Scoping every
        // keyword-based match below to just these two detail blocks (with their own "Total .../Net
        // Charges" subtotal lines stripped out) is what makes generic keyword matching safe — a brand
        // new label this bill has never shown before still gets picked up automatically as long as it's
        // inside the real detail section, without also picking up that unrelated breakdown page.
        var (pgeSection, pgeAdjustments) = ExtractChargeDetailSection(text, "Delivery", logFile, billPdfFileName);
        var (generationSection, generationAdjustments) = ExtractChargeDetailSection(text, "Generation", logFile, billPdfFileName);

        // A simpler bill with no CCA/community-energy provider (no PG&E/Delivery vs CCA/Generation
        // split at all) just has one plain "Details of Electric Charges" ... "Total Electric Charges"
        // block instead — no "Delivery" or "Generation" qualifier word anywhere. This fallback is only
        // tried when neither of the above matched anything, so it never overrides the more specific
        // Delivery/Generation sections on a bill that actually has them.
        var (plainSection, plainAdjustments) = pgeSection.Length == 0 && generationSection.Length == 0
            ? ExtractChargeDetailSection(text, "", logFile, billPdfFileName)
            : ("", 0m);

        var chargesScope = string.Join('\n', new[] { pgeSection, generationSection, plainSection }.Where(s => s.Length > 0));
        if (chargesScope.Length == 0)
            logFile?.Append($"PdfBillParserHelper: could not locate a 'Details of ... Electric Delivery/Generation Charges' section in {billPdfFileName} — Electricity/Credit/Tax/Other charge totals will be 0 for this bill.");

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
        record.ElectricityCharges = SumAllMatches(chargesScope,
            @"(?:Off[- ]?Peak(?:\s*-?\s*(?:Summer|Winter))?|Peak(?:\s*-?\s*(?:Summer|Winter))?)\s+[\d.,]+\s*kWh\s*@\s*\$?[\d.]+\s*\$?(-?[\d,]+\.\d{2})");

        // Credits: rather than a fixed list of known credit labels, any label ending in the word
        // "Credit" is treated as one — a label this bill has never shown before still gets picked up
        // automatically. This also naturally covers a per-kWh credit line (e.g. "Baseline Credit
        // 465.00 kWh @-$0.0814 -37.85" or "MCE Cost Relief Credit 2,278.06 kWh @$0.0062 -14.12") via
        // the same optional kWh clause used for ElectricityCharges, taking the trailing dollar amount
        // rather than the kWh figure. "Bright Choice" is kept as an explicit addition since it's a
        // real credit line that doesn't literally contain the word "Credit".
        // The bill itself shows these as negative amounts, written either as "-357.25" or as
        // "-$36.18" (minus before the dollar sign, seen on a real "California Climate Credit" line) —
        // the trailing amount capture allows an optional "-" on both sides of an optional "$" to catch
        // either order, and the Excel output keeps that same negative sign rather than flipping it
        // positive.
        // Some bills also add a short "Adjustments" mini-block right after an ELECTRIC section's own
        // Total line (e.g. "Adjustments \n California Climate Credit -$36.18 \n CA Climate Credit City
        // Franchise Surcharge Adjustment -$0.11 \n Total Adjustments -$36.29"), reflected in the
        // Account Summary's own "Electric Adjustments -36.29" line — real, un-duplicated credit data
        // that would otherwise go uncounted since it falls just outside the section boundary above.
        // pgeAdjustments/generationAdjustments/plainAdjustments come from ExtractChargeDetailSection
        // itself, scoped to right after THAT specific Electric section's own Total line — a different
        // real bill has its own unrelated "Adjustments"/"Total Adjustments" block (a "Refund Draft")
        // inside "Details of Gas Charges" instead, which must never be counted here (it's not an
        // electric credit); since ExtractChargeDetailSection is only ever called for the Electric
        // Delivery/Generation/plain sections, never for Gas, this scoping keeps that Gas-side
        // adjustment out automatically rather than needing its own exclusion logic.
        record.CreditReceived = SumAllMatches(chargesScope,
                @"[A-Za-z][A-Za-z'()%.\- ]*?Credit(?:[ \t]+[\d.,]+\s*kWh[ \t]*@[ \t]*-?\$?-?[\d.]+)?[ \t]+(-?\$?-?[\d,]+\.\d{2})")
            + SumAllMatches(chargesScope, @"Bright Choice[ \t]+(-?\$?-?[\d,]+\.\d{2})")
            + pgeAdjustments + generationAdjustments + plainAdjustments;

        // Taxes: any label ending in "Tax" is summed generically (city utility users' taxes vary by
        // name — Stockton, Fairfield, San Francisco, etc. — and this also picks up "Energy Commission
        // Tax" with no separate pattern needed for it). Two additions cover wording that doesn't
        // literally end in "Tax": "Energy Commission Surcharge" (some CCAs call the same line item a
        // surcharge instead of a tax) and any "<label> Tax Surcharge" ending (e.g. San Francisco's own
        // "SF Prop C Tax Surcharge", levied on top of its Utility Users' Tax).
        // A tax line is followed by a "(6.000%)" rate parenthetical before the actual dollar amount —
        // that parenthetical must be consumed explicitly, otherwise the trailing-amount match stops at
        // the first digit inside "(6.000%)" and captures a truncated fragment of the percentage itself
        // instead of the real amount after it.
        record.TotalTaxAmount = SumAllMatches(chargesScope, @"[A-Za-z][A-Za-z'()%.\- ]*?Tax(?:\s+Surcharge)?(?:\s*\(\s*[\d.]+\s*%\s*\))?[ \t]+\$?(-?[\d,]+\.\d{2})")
            + SumAllMatches(chargesScope, @"Energy Commission Surcharge[ \t]+\$?(-?[\d,]+\.\d{2})");

        // Everything else: any label containing the word "Charge"/"Charges"/"Surcharge" anywhere in it
        // — not just as the label's last word — covers "Customer Charge"/"Base Services Charge" (the
        // flat per-period charge) as well as "Power Charge Indifference Adjustment" and "Franchise Fee
        // Surcharge", where "Charge"/"Surcharge" sits in the middle or end of a longer label. A label
        // ending in "...Credit" (e.g. "Power Charge Indifference Adjustment Credit") is explicitly
        // excluded at every trailing word so it's only ever counted once, by the Credit pattern above.
        // A "Surcharge" immediately preceded by "Tax" or "Commission" (e.g. "SF Prop C Tax Surcharge",
        // "Energy Commission Surcharge") is excluded the same way — those are taxes, already counted by
        // TotalTaxAmount above, not a separate charge.
        record.OtherCharges = SumAllMatches(chargesScope,
            @"\b(?<!Tax\s)(?<!Commission\s)(?:Charges?|Surcharge)\b(?:[ \t]+\d+\s*days?\s*@\s*\$?[\d.]+)?(?:[ \t]+[\d.,]+\s*kWh\s*@\s*\$?[\d.]+)?(?:[ \t]+(?!Credit\b)[A-Za-z]+)*?[ \t]+\$?(-?[\d,]+\.\d{2})");

        return record;
    }

    // Bounds the text to just the itemized charge lines for one side of the bill ("Delivery" for
    // PG&E's own page, "Generation" for the 3rd-party CCA/community-energy page, or "" for a simpler
    // bill with no CCA at all — just a plain "Details of Electric Charges" section, no "Delivery"/
    // "Generation" qualifier) — from the "Details of <Provider> Electric [<kind>] Charges" header up
    // to (not including) its own "Total <Provider> Electric [<kind>] Charges" line. The provider name
    // in between ("PG&E", "MCE", "Ava", "CleanPowerSF", etc.) is matched generically rather than
    // hardcoded, since it varies per bill/CCA.
    // The gap between "of"/"Total" and "Electric" tolerates a run of unrelated text (not just
    // whitespace) for the same reason as below. When a kind word IS given, the gap between "Electric"
    // and it (and between it and "Charges") is tolerant too — confirmed against a real bill's actual
    // extracted text that the "Total <Provider> Electric" / "Generation Charges $126.76" end heading
    // can have an entire unrelated sidebar line ("For questions regarding charges on this page,")
    // interleaved between "Electric" and "Generation" by the Y-position-based line grouping in
    // ExtractText (that sidebar column sits at the same page height as this heading). When there's no
    // kind word to anchor on (the plain "Electric Charges" case), the gap right before "Charges" is
    // kept tight instead — a wide tolerance there with no anchor word would risk matching across to
    // an unrelated later "Electric ... Charges" phrase, e.g. on the "Your Electric Charges Breakdown"
    // summary page.
    private static (string Section, decimal AdjustmentsTotal) ExtractChargeDetailSection(string text, string kind, AppLogFile? logFile, string billPdfFileName)
    {
        var kindGap = string.IsNullOrEmpty(kind) ? "" : $@"[\s\S]{{0,80}}?{kind}";
        var chargesGap = string.IsNullOrEmpty(kind) ? @"\s*" : @"[\s\S]{0,40}?";

        var startMatch = Regex.Match(text, $@"Details\s+of[\s\S]{{0,80}}?Electric{kindGap}{chargesGap}Charges", RegexOptions.IgnoreCase);
        if (!startMatch.Success)
            return ("", 0m);

        var searchFrom = startMatch.Index + startMatch.Length;
        var remainder = text[searchFrom..];
        var endMatch = Regex.Match(remainder, $@"Total[\s\S]{{0,80}}?Electric{kindGap}{chargesGap}Charges", RegexOptions.IgnoreCase);

        // If this section's own "Total ... Charges" line can't be found (e.g. the same sidebar-merge
        // issue as the start header, or wording this hasn't seen before), the section is NOT extended
        // unboundedly to the rest of the document — that previously leaked into the Gas Charges
        // section and the unrelated "Your Electric Charges Breakdown" summary page (both of which
        // contain plenty of their own "Charge"/"Credit"/"Tax"-worded lines), silently inflating every
        // generic-keyword total below. Coming back empty is the safe failure mode; it's logged so a
        // genuinely new bill layout can still be diagnosed and fixed instead of miscounted.
        if (!endMatch.Success)
        {
            var kindLabel = string.IsNullOrEmpty(kind) ? "(plain)" : kind;
            logFile?.Append($"ExtractChargeDetailSection({kindLabel}): found the section start in {billPdfFileName} but not its own 'Total ... Electric ... Charges' end line — returning nothing for this section rather than risk scanning past it.");
            return ("", 0m);
        }

        var section = remainder[..endMatch.Index];

        // Some bills add a short "Adjustments" mini-block immediately after THIS section's own Total
        // line (e.g. "Adjustments \n California Climate Credit -$36.18 \n ... \n Total Adjustments
        // -$36.29"), before "Service Information" starts — real, un-duplicated credit data reflected
        // in the Account Summary's own "Electric Adjustments -36.29" line. Rather than trying to sum
        // its individual lines (a real bill's line-reconstruction split a label and its amount onto
        // two separate lines here, since unrelated sidebar text got interleaved between them), this
        // takes the block's own clean "Total Adjustments <amount>" line directly. The lookahead is
        // capped at ~400 chars so it only ever matches a block right after this section's own Total
        // line — scoped to THIS call's Electric section specifically, so a Gas Charges section's own
        // unrelated "Adjustments"/"Total Adjustments" block (e.g. a "Refund Draft") is never reached,
        // since this method is never invoked for Gas.
        var afterTotal = remainder[endMatch.Index..];
        var adjustmentsMatch = Regex.Match(afterTotal, @"\bAdjustments\b[\s\S]{0,400}?\bTotal\s+Adjustments\b[^\n]*?(-?\$?-?[\d,]+\.\d{2})", RegexOptions.IgnoreCase);
        var adjustmentsTotal = 0m;
        if (adjustmentsMatch.Success)
        {
            var rawValue = adjustmentsMatch.Groups[1].Value.Replace(",", "").Replace("$", "");
            decimal.TryParse(rawValue, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out adjustmentsTotal);
        }

        // Any "Total ..."/"Net Charges" line remaining inside that span (a sub-rollup of the same
        // section, e.g. "Net Charges 365.68") is stripped out — otherwise the generic keyword patterns
        // above would double-count it on top of the individual line items it's a sum of.
        var keptLines = section.Split('\n').Where(line => !Regex.IsMatch(line.TrimStart(), @"^(Total\b|Net Charges\b)", RegexOptions.IgnoreCase));
        return (string.Join('\n', keptLines), adjustmentsTotal);
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
