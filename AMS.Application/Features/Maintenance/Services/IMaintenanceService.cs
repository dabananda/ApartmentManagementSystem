using AMS.Domain.Entities;

using AMS.Application.Features.Home.DTOs;

namespace AMS.Application.Features.Maintenance.Services;

public interface IMaintenanceService
{
    Task<IReadOnlyList<MaintenanceTicket>> GetForBuildingAsync(Guid buildingId, string? status, CancellationToken cancellationToken = default);
    Task CreateAsync(MaintenanceTicket ticket, Guid buildingId, CancellationToken cancellationToken = default);
    Task<MaintenanceTicket?> AdvanceAsync(Guid id, Guid buildingId, CancellationToken cancellationToken = default);
}
