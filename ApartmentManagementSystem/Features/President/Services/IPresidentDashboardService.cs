using ApartmentManagementSystem.Features.President.ViewModels;

namespace ApartmentManagementSystem.Features.President.Services;
public interface IPresidentDashboardService { Task<PresidentDashboardViewModel> GetAsync(Guid buildingId, CancellationToken cancellationToken = default); }
