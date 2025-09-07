namespace ApartmentManagementSystem.ViewModels.Reports
{
    public class FinancialReportRow
    {
        public Guid CommonBillId { get; set; }
        public string Title { get; set; } = "";
        public DateTime BillDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Collected { get; set; }
        public decimal Payments { get; set; }
        public decimal Outstanding => Math.Max(TotalAmount - Collected, 0m);
    }

    public class FinancialReportViewModel
    {
        public string BuildingName { get; set; } = "";
        public Reports.DateRangeFilter Filter { get; set; } = new Reports.DateRangeFilter();

        public decimal TotalBills { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal NetBalance => TotalCollected - TotalPayments;

        public List<FinancialReportRow> Rows { get; set; } = new();
    }
}
