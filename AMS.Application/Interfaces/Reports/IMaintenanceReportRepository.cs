using AMS.Domain.Entities;
using AMS.Application.Features.Home.DTOs;

namespace AMS.Application.Interfaces.Reports;

public interface IMaintenanceReportRepository
{
    Task<MaintenanceSummary> GetSummaryAsync(Guid buildingId, DateTime start, DateTime endExclusive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceTicket>> GetCsvRowsAsync(Guid buildingId, DateTime start, DateTime endExclusive, CancellationToken cancellationToken = default);
}

public sealed record MaintenanceSummary(
    int Open,
    int InProgress,
    int Closed,
    int CreatedInRange,
    int ClosedInRange,
    double? AvgResolutionHours);
