using AMS.Application.Features.Reports.DTOs;
using AMS.Domain.Entities;

using AMS.Application.Features.Home.DTOs;
namespace AMS.Application.Features.Reports.Services;

public interface IPresidentVisitorReportService
{
    Task<VisitorReportViewModel> GetAsync(Guid buildingId, string buildingName, DateRangeFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EntryLog>> GetCsvAsync(Guid buildingId, DateRangeFilter filter, CancellationToken cancellationToken = default);
}
