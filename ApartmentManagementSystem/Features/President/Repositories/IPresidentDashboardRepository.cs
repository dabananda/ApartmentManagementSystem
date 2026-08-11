using ApartmentManagementSystem.Features.President.ViewModels;

namespace ApartmentManagementSystem.Features.President.Repositories;
public interface IPresidentDashboardRepository
{
    Task<PresidentDashboardViewModel> GetAsync(Guid buildingId, CancellationToken cancellationToken = default);
}
