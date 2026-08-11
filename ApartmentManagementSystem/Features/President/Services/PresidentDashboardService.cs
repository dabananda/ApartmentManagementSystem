using ApartmentManagementSystem.Features.President.Repositories;
using ApartmentManagementSystem.ViewModels.President;

namespace ApartmentManagementSystem.Features.President.Services;
public sealed class PresidentDashboardService(IPresidentDashboardRepository dashboard) : IPresidentDashboardService
{
    public Task<PresidentDashboardViewModel> GetAsync(Guid buildingId, CancellationToken cancellationToken = default) => dashboard.GetAsync(buildingId, cancellationToken);
}
