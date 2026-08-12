using System.ComponentModel.DataAnnotations;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Buildings.DTOs;

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
        return Building.Create(
            name: Name,
            code: Code ?? string.Empty,
            address: Address
        );
    }
}
