using AMS.Application.Features.Reports.DTOs;
using AMS.Application.Interfaces.Reports;

namespace AMS.Application.Features.Reports.Services;

public interface IPresidentOccupancyReportService
{
    Task<OccupancyReportViewModel> GetAsync(Guid buildingId, string buildingName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OccupancyFlatRow>> GetCsvAsync(Guid buildingId, CancellationToken cancellationToken = default);
}
