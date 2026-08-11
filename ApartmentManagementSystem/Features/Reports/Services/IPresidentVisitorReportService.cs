using ApartmentManagementSystem.Features.Reports.ViewModels;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
namespace ApartmentManagementSystem.Features.Reports.Services;
public interface IPresidentVisitorReportService
{
    Task<VisitorReportViewModel> GetAsync(Guid buildingId, string buildingName, DateRangeFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EntryLog>> GetCsvAsync(Guid buildingId, DateRangeFilter filter, CancellationToken cancellationToken = default);
}
