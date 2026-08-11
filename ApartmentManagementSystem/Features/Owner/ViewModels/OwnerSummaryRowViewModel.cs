namespace ApartmentManagementSystem.Features.Owner.ViewModels
{
    public class OwnerSummaryRowViewModel
    {
        public string OwnerId { get; set; } = default!;
        public string OwnerName { get; set; } = default!;
        public string FlatsCsv { get; set; } = string.Empty;

        public decimal TotalCommonBills { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalDue => TotalCommonBills - TotalPaid;
    }
}
