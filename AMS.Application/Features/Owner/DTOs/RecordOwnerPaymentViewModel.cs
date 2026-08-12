using System.ComponentModel.DataAnnotations;

namespace AMS.Application.Features.Owner.DTOs;

public class RecordOwnerPaymentViewModel
{
    [Required]
    public Guid CommonBillId { get; set; }

    [Required]
    public string OwnerId { get; set; } = default!;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [DataType(DataType.Date)]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [StringLength(100)]
    public string? Reference { get; set; }

    public string OwnerName { get; set; } = string.Empty;
    public decimal CurrentDue { get; set; }
}
