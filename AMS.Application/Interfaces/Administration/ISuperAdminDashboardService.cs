using AMS.Application.Features.Administration.DTOs;

namespace AMS.Application.Interfaces.Administration;

public interface ISuperAdminDashboardService
{
    Task<SuperAdminDashboardViewModel> GetAsync(CancellationToken cancellationToken = default);
}
