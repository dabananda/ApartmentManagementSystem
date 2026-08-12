using ApartmentManagementSystem.Application.Interfaces.President;
using ApartmentManagementSystem.Application.Features.President.DTOs;

namespace ApartmentManagementSystem.Application.Features.President.Services;

public sealed class PresidentDashboardService(IPresidentDashboardRepository dashboard) : IPresidentDashboardService
{
    public Task<PresidentDashboardViewModel> GetAsync(Guid buildingId, CancellationToken cancellationToken = default) => dashboard.GetAsync(buildingId, cancellationToken);
}
