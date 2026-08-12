using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMS.Domain.Entities;

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
    public string? Reference { get; set; }

    [Required]
    public Guid CommonBillId { get; set; }

    [Required]
    public string OwnerId { get; set; } = default!;

    [Required]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [StringLength(80)]
    public string? IdempotencyKey { get; set; }

    public PaymentGateway Gateway { get; set; } = PaymentGateway.None;

    [StringLength(120)]
    public string? ExternalRef { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Succeeded;
}
