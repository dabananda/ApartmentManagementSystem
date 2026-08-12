using AMS.Application.Features.President.DTOs;

namespace AMS.Application.Interfaces.President;

public interface IPresidentDashboardRepository
{
    Task<PresidentDashboardViewModel> GetAsync(Guid buildingId, CancellationToken cancellationToken = default);
}
