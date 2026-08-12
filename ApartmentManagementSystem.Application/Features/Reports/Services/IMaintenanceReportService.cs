using ApartmentManagementSystem.Domain.Entities;

using ApartmentManagementSystem.Application.Features.Home.DTOs;
using ApartmentManagementSystem.Application.Features.Reports.DTOs;

namespace ApartmentManagementSystem.Application.Features.Reports.Services;

public interface IMaintenanceReportService
{
    Task<MaintenanceReportViewModel> GetAsync(Guid buildingId, string buildingName, DateRangeFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceCsvRow>> GetCsvAsync(Guid buildingId, DateRangeFilter filter, CancellationToken cancellationToken = default);
}

public sealed record MaintenanceCsvRow(string Title, string Status, DateTime CreatedAt, DateTime? ClosedAt);
