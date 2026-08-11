using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;

namespace ApartmentManagementSystem.Features.Reports.Repositories;

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
