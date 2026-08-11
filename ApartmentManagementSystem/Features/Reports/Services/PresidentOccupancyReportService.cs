using ApartmentManagementSystem.Features.Reports.Repositories;
using ApartmentManagementSystem.Features.Reports.ViewModels;

namespace ApartmentManagementSystem.Features.Reports.Services;
public sealed class PresidentOccupancyReportService(IPresidentOccupancyReportRepository reports) : IPresidentOccupancyReportService
{
    public async Task<OccupancyReportViewModel> GetAsync(Guid buildingId, string buildingName, CancellationToken cancellationToken = default)
    {
        var (total, occupied, owners, tenants) = await reports.GetSummaryAsync(buildingId, cancellationToken);
        return new OccupancyReportViewModel { BuildingName = buildingName, TotalFlats = total, OccupiedFlats = occupied, OwnersCount = owners, TenantsCount = tenants };
    }
    public Task<IReadOnlyList<OccupancyFlatRow>> GetCsvAsync(Guid buildingId, CancellationToken cancellationToken = default) => reports.GetFlatsAsync(buildingId, cancellationToken);
}
