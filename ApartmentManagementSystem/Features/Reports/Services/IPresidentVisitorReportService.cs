using ApartmentManagementSystem.ViewModels.Reports;
using ApartmentManagementSystem.Models;
namespace ApartmentManagementSystem.Features.Reports.Services;
public interface IPresidentVisitorReportService
{
    Task<VisitorReportViewModel> GetAsync(Guid buildingId, string buildingName, DateRangeFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EntryLog>> GetCsvAsync(Guid buildingId, DateRangeFilter filter, CancellationToken cancellationToken = default);
}
