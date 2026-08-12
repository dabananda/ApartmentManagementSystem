using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMS.Domain.Entities
{
    public enum PaymentGateway
    {
        None = 0,
        Stripe = 1
    }

    public enum PaymentStatus
    {
        Succeeded = 0,
        Pending = 1,
        Failed = 2,
        Refunded = 3
    }

    public class TenantPayment
    {
        public Guid Id { get; set; }

        [Required] public Guid TenantBillId { get; set; }
        [ForeignKey(nameof(TenantBillId))] public TenantBill? TenantBill { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [StringLength(100)]
        public string? Reference { get; set; }

        [StringLength(80)]
        public string? IdempotencyKey { get; set; }

        public PaymentGateway Gateway { get; set; } = PaymentGateway.None;

        [StringLength(120)]
        public string? ExternalRef { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Succeeded;
    }
}
