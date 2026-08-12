using System.ComponentModel.DataAnnotations;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Buildings.DTOs;

public class BuildingEditViewModel
{
    [Required]
    public Guid Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Address { get; set; } = string.Empty;

    public void UpdateEntity(Building building)
    {
        building.UpdateDetails(
            name: Name,
            code: building.Code, // Code is not edited in this view model, keep existing
            address: Address
        );
    }

    public static BuildingEditViewModel FromEntity(Building building)
    {
        return new BuildingEditViewModel
        {
            Id = building.Id,
            Name = building.Name,
            Address = building.Address ?? string.Empty
        };
    }
}
