using ApartmentManagementSystem.Models;

namespace ApartmentManagementSystem.Features.Maintenance.Services;

public interface IMaintenanceService
{
    Task<IReadOnlyList<MaintenanceTicket>> GetForBuildingAsync(Guid buildingId, string? status, CancellationToken cancellationToken = default);
    Task CreateAsync(MaintenanceTicket ticket, Guid buildingId, CancellationToken cancellationToken = default);
    Task<MaintenanceTicket?> AdvanceAsync(Guid id, Guid buildingId, CancellationToken cancellationToken = default);
}
