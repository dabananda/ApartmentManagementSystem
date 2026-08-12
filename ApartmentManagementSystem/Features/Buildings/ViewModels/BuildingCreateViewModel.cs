using System.ComponentModel.DataAnnotations;
using ApartmentManagementSystem.Domain.Entities;

namespace ApartmentManagementSystem.Features.Buildings.ViewModels;

public class BuildingCreateViewModel
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Code { get; set; }

    public Building ToEntity()
    {
        return new Building
        {
            Name = Name,
            Address = Address,
            Code = Code ?? string.Empty
        };
    }
}
