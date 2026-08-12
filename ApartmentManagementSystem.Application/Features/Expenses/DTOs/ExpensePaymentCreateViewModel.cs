using System.ComponentModel.DataAnnotations;
using ApartmentManagementSystem.Domain.Entities;

namespace ApartmentManagementSystem.Application.Features.Expenses.DTOs;

public class ExpensePaymentCreateViewModel
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Required, Range(0.01, 1000000000)]
    public decimal Amount { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Required]
    public Guid BuildingId { get; set; }

    [Required]
    public Guid CommonBillId { get; set; }

    public ExpensePayment ToEntity()
    {
        return new ExpensePayment
        {
            Name = Name,
            PaymentDate = PaymentDate,
            Amount = Amount,
            Notes = Notes,
            BuildingId = BuildingId,
            CommonBillId = CommonBillId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
