using System.ComponentModel.DataAnnotations;
using ApartmentManagementSystem.Domain.Entities;

namespace ApartmentManagementSystem.Application.Features.Expenses.DTOs;

public class CommonBillEditViewModel
{
    [Required]
    public Guid Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, Range(0.01, 1000000000)]
    public decimal TotalAmount { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Required]
    public Guid BuildingId { get; set; }

    public DateTime BillDate { get; set; }

    public void UpdateEntity(CommonBill bill)
    {
        bill.Name = Name;
        bill.TotalAmount = TotalAmount;
        bill.Notes = Notes;
    }

    public static CommonBillEditViewModel FromEntity(CommonBill bill)
    {
        return new CommonBillEditViewModel
        {
            Id = bill.Id,
            Name = bill.Name,
            TotalAmount = bill.TotalAmount,
            Notes = bill.Notes,
            BuildingId = bill.BuildingId,
            BillDate = bill.BillDate
        };
    }
}
