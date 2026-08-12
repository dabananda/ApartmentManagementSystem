using ApartmentManagementSystem.Domain.Entities;

namespace ApartmentManagementSystem.Features.Buildings.ViewModels;

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
            Name = building.Name,
            Address = building.Address,
            Code = building.Code
        };
    }
}
