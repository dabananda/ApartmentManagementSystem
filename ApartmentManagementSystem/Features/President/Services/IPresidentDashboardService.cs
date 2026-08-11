using ApartmentManagementSystem.ViewModels.President;

namespace ApartmentManagementSystem.Features.President.Services;
public interface IPresidentDashboardService { Task<PresidentDashboardViewModel> GetAsync(Guid buildingId, CancellationToken cancellationToken = default); }
