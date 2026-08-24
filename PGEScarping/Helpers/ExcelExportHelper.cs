using ClosedXML.Excel;
using PGEScarping.Models;

namespace PGEScarping.Helpers;

public static class ExcelExportHelper
{
    private static readonly string[] Columns =
    [
        "Account Number", "Account Name", "Statement Date", "Due Date",
        "Total Bill Amount", "Electricity Charges", "Credit Received",
        "Total Tax Amount", "Other Charges", "Gas Charges", "Total Usage (kWh)", "Bill PDF File"
    ];

    // Reads back a previously-written workbook so a new run can merge into it instead of overwriting
    // it — the columns are written in the exact order/format WriteWorkbook uses, so this is the
    // inverse of that method.
    public static List<PgeBillRecord> ReadExistingRecords(string filePath)
    {
        var bills = new List<PgeBillRecord>();
        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheets.First();

        var row = 2;
        while (!ws.Cell(row, 1).IsEmpty())
        {
            bills.Add(new PgeBillRecord
            {
                AccountNumber = ws.Cell(row, 1).GetString(),
                AccountName = ws.Cell(row, 2).GetString(),
                StatementDate = DateTime.TryParse(ws.Cell(row, 3).GetString(), out var statementDate) ? statementDate : null,
                DueDate = DateTime.TryParse(ws.Cell(row, 4).GetString(), out var dueDate) ? dueDate : null,
                TotalBillAmount = ws.Cell(row, 5).GetValue<decimal>(),
                ElectricityCharges = ws.Cell(row, 6).GetValue<decimal>(),
                CreditReceived = ws.Cell(row, 7).GetValue<decimal>(),
                TotalTaxAmount = ws.Cell(row, 8).GetValue<decimal>(),
                OtherCharges = ws.Cell(row, 9).GetValue<decimal>(),
                GasCharges = ws.Cell(row, 10).GetValue<decimal>(),
                TotalUsageKwh = ws.Cell(row, 11).GetValue<decimal>(),
                BillPdfFileName = ws.Cell(row, 12).GetString()
            });
            row++;
        }

        return bills;
    }

    // Merges newly-scraped bills into whatever's already in the output file (keyed by account +
    // statement date), so running the scraper again — especially for just one account via the
    // account-number override — adds to the workbook instead of wiping out every other account's data.
    public static List<PgeBillRecord> MergeWithExisting(string filePath, List<PgeBillRecord> newBills)
    {
        var existing = File.Exists(filePath) ? ReadExistingRecords(filePath) : [];

        var merged = new Dictionary<string, PgeBillRecord>();
        foreach (var bill in existing.Concat(newBills))
            merged[$"{bill.AccountNumber}|{bill.StatementDate:yyyy-MM-dd}"] = bill;

        return merged.Values
            .OrderBy(b => b.AccountNumber)
            .ThenBy(b => b.StatementDate)
            .ToList();
    }

    public static void WriteWorkbook(string filePath, List<PgeBillRecord> bills, string sheetName = "PGE Billing History")
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(sheetName.Length > 31 ? sheetName[..31] : sheetName);

        for (var c = 0; c < Columns.Length; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = Columns[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        for (var r = 0; r < bills.Count; r++)
        {
            var bill = bills[r];
            var row = r + 2;

            ws.Cell(row, 1).Value = bill.AccountNumber;
            ws.Cell(row, 2).Value = bill.AccountName;
            ws.Cell(row, 3).Value = bill.StatementDate?.ToShortDateString() ?? "";
            ws.Cell(row, 4).Value = bill.DueDate?.ToShortDateString() ?? "";
            ws.Cell(row, 5).Value = bill.TotalBillAmount;
            ws.Cell(row, 6).Value = bill.ElectricityCharges;
            ws.Cell(row, 7).Value = bill.CreditReceived;
            ws.Cell(row, 8).Value = bill.TotalTaxAmount;
            ws.Cell(row, 9).Value = bill.OtherCharges;
            ws.Cell(row, 10).Value = bill.GasCharges;
            ws.Cell(row, 11).Value = bill.TotalUsageKwh;
            ws.Cell(row, 12).Value = bill.BillPdfFileName;

            if (r % 2 == 1)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
        }

        if (bills.Count > 0)
            ws.Range(1, 1, 1, Columns.Length).SetAutoFilter();

        ws.Columns().AdjustToContents();

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        wb.SaveAs(filePath);
    }
}
