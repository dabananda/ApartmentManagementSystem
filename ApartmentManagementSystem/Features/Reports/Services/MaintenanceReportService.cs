using ApartmentManagementSystem.Features.Reports.Repositories;
using ApartmentManagementSystem.Features.Reports.ViewModels;

namespace ApartmentManagementSystem.Features.Reports.Services;

public sealed class MaintenanceReportService(IMaintenanceReportRepository reports) : IMaintenanceReportService
{
    public async Task<MaintenanceReportViewModel> GetAsync(Guid buildingId, string buildingName, DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var (start, endExclusive) = filter.ToBoundsOrDefault(90);
        var summary = await reports.GetSummaryAsync(buildingId, start, endExclusive, cancellationToken);

        return new MaintenanceReportViewModel
        {
            BuildingName = buildingName,
            Filter = filter,
            OpenCount = summary.Open,
            InProgressCount = summary.InProgress,
            ClosedCount = summary.Closed,
            AvgResolutionHours = summary.AvgResolutionHours,
            NewlyCreated = summary.CreatedInRange,
            ClosedInRange = summary.ClosedInRange
        };
    }

    public async Task<IReadOnlyList<MaintenanceCsvRow>> GetCsvAsync(Guid buildingId, DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var (start, endExclusive) = filter.ToBoundsOrDefault(90);
        var rows = await reports.GetCsvRowsAsync(buildingId, start, endExclusive, cancellationToken);
        return rows.Select(r => new MaintenanceCsvRow(r.Title, r.Status, r.CreatedAt, r.ClosedAt)).ToList();
    }
}
