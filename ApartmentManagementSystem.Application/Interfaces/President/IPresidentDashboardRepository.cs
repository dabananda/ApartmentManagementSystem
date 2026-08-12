using ApartmentManagementSystem.Application.Features.President.DTOs;

namespace ApartmentManagementSystem.Application.Interfaces.President;

public interface IPresidentDashboardRepository
{
    Task<PresidentDashboardViewModel> GetAsync(Guid buildingId, CancellationToken cancellationToken = default);
}
