namespace AMS.Application.Features.Buildings.DTOs;

public class BuildingIndexPageViewModel
{
    public BuildingIndexFilterViewModel Filter { get; set; } = new();
    public IEnumerable<BuildingListItemViewModel> Buildings { get; set; } = [];
    public int Total { get; set; }
}


