using ApartmentManagementSystem.Features.Reports.ViewModels;
using ApartmentManagementSystem.Features.Reports.Repositories;

namespace ApartmentManagementSystem.Features.Reports.Services;
public interface IPresidentOccupancyReportService
{
    Task<OccupancyReportViewModel> GetAsync(Guid buildingId, string buildingName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OccupancyFlatRow>> GetCsvAsync(Guid buildingId, CancellationToken cancellationToken = default);
}
