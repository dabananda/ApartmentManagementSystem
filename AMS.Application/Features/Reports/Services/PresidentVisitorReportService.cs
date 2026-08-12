using AMS.Application.Interfaces.Reports;
using AMS.Domain.Entities;

using AMS.Application.Features.Home.DTOs;
using AMS.Application.Features.Reports.DTOs;
namespace AMS.Application.Features.Reports.Services;

public sealed class PresidentVisitorReportService(IPresidentVisitorReportRepository reports) : IPresidentVisitorReportService
{
    public async Task<VisitorReportViewModel> GetAsync(Guid buildingId, string buildingName, DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var (start, end) = filter.ToBoundsOrDefault(30); var entries = await reports.GetAsync(buildingId, start, end, cancellationToken);
        return new VisitorReportViewModel { BuildingName = buildingName, Filter = filter, TotalEntries = entries.Count, ByCategory = entries.GroupBy(entry => entry.EntryType).ToDictionary(group => group.Key.ToString(), group => group.Count()), DailyCounts = entries.GroupBy(entry => entry.EntryTime.Date).OrderBy(group => group.Key).Select(group => (group.Key, group.Count())).ToList() };
    }
    public Task<IReadOnlyList<EntryLog>> GetCsvAsync(Guid buildingId, DateRangeFilter filter, CancellationToken cancellationToken = default) { var (start, end) = filter.ToBoundsOrDefault(30); return reports.GetAsync(buildingId, start, end, cancellationToken); }
}
