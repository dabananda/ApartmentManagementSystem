using ApartmentManagementSystem.Features.Administration.ViewModels;

namespace ApartmentManagementSystem.Features.Administration.Services;
public interface ISuperAdminDashboardService
{
    Task<SuperAdminDashboardViewModel> GetAsync(CancellationToken cancellationToken = default);
}
