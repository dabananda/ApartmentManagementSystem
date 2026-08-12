using AMS.Domain.Entities;

namespace AMS.Application.Features.Buildings.DTOs;

public class BuildingDetailsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public static BuildingDetailsViewModel FromEntity(Building building)
    {
        return new BuildingDetailsViewModel
        {
            Id = building.Id,
            Name = building.Name ?? string.Empty,
            Address = building.Address ?? string.Empty,
            Code = building.Code ?? string.Empty
        };
    }
}
