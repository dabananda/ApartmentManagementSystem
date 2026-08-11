using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;

namespace ApartmentManagementSystem.Features.Maintenance.Repositories;

public interface IMaintenanceTicketRepository
{
    Task<IReadOnlyList<MaintenanceTicket>> GetByBuildingAsync(Guid buildingId, string? status, CancellationToken cancellationToken = default);
    Task<MaintenanceTicket?> GetAsync(Guid id, Guid buildingId, CancellationToken cancellationToken = default);
    Task AddAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
