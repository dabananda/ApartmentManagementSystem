using ApartmentManagementSystem.Application.Features.Reports.DTOs;
using ApartmentManagementSystem.Application.Interfaces.Reports;

namespace ApartmentManagementSystem.Application.Features.Reports.Services;

public interface IPresidentOccupancyReportService
{
    Task<OccupancyReportViewModel> GetAsync(Guid buildingId, string buildingName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OccupancyFlatRow>> GetCsvAsync(Guid buildingId, CancellationToken cancellationToken = default);
}
