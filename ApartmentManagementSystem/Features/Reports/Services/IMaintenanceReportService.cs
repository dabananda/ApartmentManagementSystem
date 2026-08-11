using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using ApartmentManagementSystem.Features.Reports.ViewModels;

namespace ApartmentManagementSystem.Features.Reports.Services;

public interface IMaintenanceReportService
{
    Task<MaintenanceReportViewModel> GetAsync(Guid buildingId, string buildingName, DateRangeFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceCsvRow>> GetCsvAsync(Guid buildingId, DateRangeFilter filter, CancellationToken cancellationToken = default);
}

public sealed record MaintenanceCsvRow(string Title, string Status, DateTime CreatedAt, DateTime? ClosedAt);
