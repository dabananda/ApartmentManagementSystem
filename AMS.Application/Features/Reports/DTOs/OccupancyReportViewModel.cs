namespace AMS.Application.Features.Reports.DTOs;

public class OccupancyReportViewModel
{
    public string BuildingName { get; set; } = "";
    public int TotalFlats { get; set; }
    public int OccupiedFlats { get; set; }
    public int VacantFlats => TotalFlats - OccupiedFlats;
    public decimal OccupancyRate => TotalFlats == 0 ? 0 : (decimal)OccupiedFlats / TotalFlats * 100m;

    public int OwnersCount { get; set; }
    public int TenantsCount { get; set; }
}
