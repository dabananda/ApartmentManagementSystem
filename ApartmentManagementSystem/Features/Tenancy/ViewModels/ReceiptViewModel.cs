namespace ApartmentManagementSystem.Features.Tenancy.ViewModels
{
    public class ReceiptViewModel
    {
        public Guid Id { get; set; }
        public string ReceiptNo { get; set; } = "";
        public DateTime PaidOn { get; set; }
        public string OwnerName { get; set; } = "";
        public string? OwnerEmail { get; set; }
        public Guid BuildingId { get; set; }
        public string BillTitle { get; set; } = "";
        public DateTime BillDate { get; set; }
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
        public string? BuildingName { get; set; }
        public string? FlatNumber { get; set; }
    }

}
