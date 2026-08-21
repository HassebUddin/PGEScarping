using ClosedXML.Excel;
using PGEScarping.Models;

namespace PGEScarping.Helpers;

public static class ExcelExportHelper
{
    private static readonly string[] Columns =
    [
        "Account Number", "Account Name", "Statement Date", "Due Date",
        "Total Bill Amount", "Electricity Charges", "Credit Received",
        "Total Tax Amount", "Other Charges", "Total Usage (kWh)", "Bill PDF File"
    ];

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
            ws.Cell(row, 10).Value = bill.TotalUsageKwh;
            ws.Cell(row, 11).Value = bill.BillPdfFileName;

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
