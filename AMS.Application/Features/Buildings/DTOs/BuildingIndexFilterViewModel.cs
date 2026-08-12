namespace AMS.Application.Features.Buildings.DTOs
{
    public class BuildingIndexFilterViewModel
    {
        public string? Query { get; set; }
        public bool? HasPresident { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
