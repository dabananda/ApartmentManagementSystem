namespace AMS.Application.Features.Reports.DTOs;

public class MaintenanceReportViewModel
{
    public string BuildingName { get; set; } = "";
    public DateRangeFilter Filter { get; set; } = new DateRangeFilter();

    public int OpenCount { get; set; }
    public int InProgressCount { get; set; }
    public int ClosedCount { get; set; }
    public double? AvgResolutionHours { get; set; }
    public int NewlyCreated { get; set; }
    public int ClosedInRange { get; set; }
}
