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

        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [StringLength(100)]
        public string? Reference { get; set; } // e.g., receipt no, note

        // Optional redundancy for fast filters
        [Required]
        public Guid CommonBillId { get; set; }

        [Required]
        public string OwnerId { get; set; } = default!;

        // ---- New: same idempotency and gateway hooks as tenant payments ----
        [StringLength(80)]
        public string? IdempotencyKey { get; set; }

        public PaymentGateway Gateway { get; set; } = PaymentGateway.None;

        [StringLength(120)]
        public string? ExternalRef { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Succeeded;
    }
}