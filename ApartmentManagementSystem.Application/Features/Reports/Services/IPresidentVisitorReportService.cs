using ApartmentManagementSystem.Application.Features.Reports.DTOs;
using ApartmentManagementSystem.Domain.Entities;

using ApartmentManagementSystem.Application.Features.Home.DTOs;
namespace ApartmentManagementSystem.Application.Features.Reports.Services;

public interface IPresidentVisitorReportService
{
    Task<VisitorReportViewModel> GetAsync(Guid buildingId, string buildingName, DateRangeFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EntryLog>> GetCsvAsync(Guid buildingId, DateRangeFilter filter, CancellationToken cancellationToken = default);
}
