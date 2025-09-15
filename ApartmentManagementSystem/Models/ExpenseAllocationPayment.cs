using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApartmentManagementSystem.Models
{
    public class ExpenseAllocationPayment
    {
        public Guid Id { get; set; }

        [Required]
        public Guid ExpenseAllocationId { get; set; }
        [ForeignKey(nameof(ExpenseAllocationId))]
        public virtual ExpenseAllocation? ExpenseAllocation { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        // Kept for reporting (date the payment applies to)
        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [StringLength(100)]
        public string? Reference { get; set; } // e.g., receipt no, note

        [Required]
        public Guid CommonBillId { get; set; }

        [Required]
        public string OwnerId { get; set; } = default!;

        // NEW: exact timestamp the row was created (used in Recent Activity)
        [Required]
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // ---- New: same idempotency and gateway hooks as tenant payments ----
        [StringLength(80)]
        public string? IdempotencyKey { get; set; }

        public PaymentGateway Gateway { get; set; } = PaymentGateway.None;

        [StringLength(120)]
        public string? ExternalRef { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Succeeded;
    }
}