namespace ApartmentManagementSystem.ViewModels
{
    public class ReceiptViewModel
    {
        public Guid PaymentId { get; set; }
        public string ReceiptNo { get; set; } = "";
        public string OwnerName { get; set; } = "";
        public string OwnerEmail { get; set; } = "";
        public string BillTitle { get; set; } = "";
        public DateTime BillDate { get; set; }
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
        public DateTime PaidOn { get; set; }
        public Guid BuildingId { get; set; }
    }
}
