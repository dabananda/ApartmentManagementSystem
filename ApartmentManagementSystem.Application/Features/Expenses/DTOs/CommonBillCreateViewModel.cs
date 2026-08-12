using System.ComponentModel.DataAnnotations;
using ApartmentManagementSystem.Domain.Entities;

namespace ApartmentManagementSystem.Application.Features.Expenses.DTOs;

public class CommonBillCreateViewModel
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, Range(0.01, 1000000000)]
    public decimal TotalAmount { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Required]
    public Guid BuildingId { get; set; }

    public CommonBill ToEntity()
    {
        return new CommonBill
        {
            Name = Name,
            TotalAmount = TotalAmount,
            Notes = Notes,
            BuildingId = BuildingId,
            BillDate = DateTime.UtcNow
        };
    }
}
