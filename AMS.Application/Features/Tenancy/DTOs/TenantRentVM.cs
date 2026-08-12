using System.ComponentModel.DataAnnotations;

namespace AMS.Application.Features.Tenancy.DTOs
{
    public class TenantBillsPage
    {
        public string TenantUserId { get; set; } = default!;
        public string TenantName { get; set; } = "";
        public string Email { get; set; } = "";
        public Guid BuildingId { get; set; }
        public List<AMS.Application.Features.TenantPortal.DTOs.TenantBillRow> Bills { get; set; } = new();
        public decimal Total => Bills.Sum(b => b.Amount);
        public decimal TotalPaid => Bills.Sum(b => b.Paid);
        public decimal TotalDue => Bills.Sum(b => b.Due);
    }

    public class RecordTenantPaymentVM
    {
        [Required] public Guid TenantBillId { get; set; }
        [Required, Range(0.01, double.MaxValue)] public decimal Amount { get; set; }
        [DataType(DataType.Date)] public DateTime PaymentDate { get; set; } = DateTime.Today;
        [StringLength(100)] public string? Reference { get; set; }
        [StringLength(80)] public string? IdempotencyKey { get; set; }
    }

    public class TenantPaymentRecord
    {
        public Guid PaymentId { get; set; }
        public DateTime PaymentDate { get; set; }
        public string BillTitle { get; set; } = "";
        public DateTime BillDate { get; set; }
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
    }
}
