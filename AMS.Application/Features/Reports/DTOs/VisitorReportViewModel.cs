namespace AMS.Application.Features.Reports.DTOs;

public class VisitorReportViewModel
{
    public string BuildingName { get; set; } = "";
    public DateRangeFilter Filter { get; set; } = new DateRangeFilter();

    public int TotalEntries { get; set; }
    public Dictionary<string, int> ByCategory { get; set; } = new();
    public List<(DateTime Day, int Count)> DailyCounts { get; set; } = new();
}
