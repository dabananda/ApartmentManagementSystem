using AMS.Application.Interfaces.President;
using AMS.Application.Features.President.DTOs;

namespace AMS.Application.Features.President.Services;

public sealed class PresidentDashboardService(IPresidentDashboardRepository dashboard) : IPresidentDashboardService
{
    public Task<PresidentDashboardViewModel> GetAsync(Guid buildingId, CancellationToken cancellationToken = default) => dashboard.GetAsync(buildingId, cancellationToken);
}
