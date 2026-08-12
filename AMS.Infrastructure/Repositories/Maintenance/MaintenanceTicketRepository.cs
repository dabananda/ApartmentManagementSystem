using AMS.Application.Interfaces.Maintenance;
using AMS.Domain.Entities;
using AMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AMS.Infrastructure.Repositories.Maintenance;

public sealed class MaintenanceTicketRepository(ApplicationDbContext context) : IMaintenanceTicketRepository
{
    public async Task<IReadOnlyList<MaintenanceTicket>> GetByBuildingAsync(Guid buildingId, string? status, CancellationToken cancellationToken = default)
    {
        var query = context.MaintenanceTickets.AsNoTracking().Where(ticket => ticket.BuildingId == buildingId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(ticket => ticket.Status == status);

        return await query.OrderBy(ticket => ticket.Status).ThenByDescending(ticket => ticket.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<MaintenanceTicket?> GetAsync(Guid id, Guid buildingId, CancellationToken cancellationToken = default) =>
        context.MaintenanceTickets.FirstOrDefaultAsync(ticket => ticket.Id == id && ticket.BuildingId == buildingId, cancellationToken);

    public async Task AddAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default)
    {
        await context.MaintenanceTickets.AddAsync(ticket, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);
}
