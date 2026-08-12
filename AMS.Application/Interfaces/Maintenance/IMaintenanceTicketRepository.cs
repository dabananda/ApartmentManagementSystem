using AMS.Domain.Entities;

namespace AMS.Application.Interfaces.Maintenance;

public interface IMaintenanceTicketRepository
{
    Task<IReadOnlyList<MaintenanceTicket>> GetByBuildingAsync(Guid buildingId, string? status, CancellationToken cancellationToken = default);
    Task<MaintenanceTicket?> GetAsync(Guid id, Guid buildingId, CancellationToken cancellationToken = default);
    Task AddAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
