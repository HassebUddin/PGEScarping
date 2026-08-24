namespace PGEScarping.Models;

public class PgeBillRecord
{
    public string AccountNumber { get; set; } = "";
    public string AccountName { get; set; } = "";
    public DateTime? StatementDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal TotalBillAmount { get; set; }
    public decimal ElectricityCharges { get; set; }
    public decimal CreditReceived { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal OtherCharges { get; set; }
    public decimal GasCharges { get; set; }
    public decimal TotalUsageKwh { get; set; }
    public string BillPdfFileName { get; set; } = "";
}
