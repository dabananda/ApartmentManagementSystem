using System.ComponentModel.DataAnnotations;

namespace ApartmentManagementSystem.ViewModels
{
    public class OwnerBillingRow
    {
        public string OwnerId { get; set; } = default!;
        public string OwnerName { get; set; } = default!;
        public string FlatsCsv { get; set; } = "";
        public decimal TotalAllocated { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalDue => TotalAllocated - TotalPaid;
    }

    public class OwnerBillItem
    {
        public Guid CommonBillId { get; set; }
        public string Title { get; set; } = "";
        public DateTime BillDate { get; set; }
        public decimal Allocated { get; set; }
        public decimal Paid { get; set; }
        public decimal Due => Allocated - Paid;
        public bool IsPaid => Due <= 0;
    }

    public class OwnerPaymentRecord
    {
        public DateTime PaymentDate { get; set; }
        public string BillTitle { get; set; } = "";
        public DateTime BillDate { get; set; }
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
    }

    public class OwnerBillsPage
    {
        public string OwnerId { get; set; } = default!;
        public string OwnerName { get; set; } = "";
        public Guid BuildingId { get; set; }
        public List<OwnerBillItem> Bills { get; set; } = new();
        public decimal TotalAllocated => Bills.Sum(b => b.Allocated);
        public decimal TotalPaid => Bills.Sum(b => b.Paid);
        public decimal TotalDue => Bills.Sum(b => b.Due);
        public List<OwnerPaymentRecord> History { get; set; } = new();
    }

    public class RecordOwnerPaymentVM
    {
        [Required] public string OwnerId { get; set; } = default!;
        [Required] public Guid CommonBillId { get; set; }
        [Required, Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
        [DataType(DataType.Date)] public DateTime PaymentDate { get; set; } = DateTime.Today;
        [StringLength(100)] public string? Reference { get; set; }
    }
}
