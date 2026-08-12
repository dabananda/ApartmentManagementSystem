using ApartmentManagementSystem.Application.Features.Administration.DTOs;

namespace ApartmentManagementSystem.Application.Interfaces.Administration;

public interface ISuperAdminDashboardService
{
    Task<SuperAdminDashboardViewModel> GetAsync(CancellationToken cancellationToken = default);
}
