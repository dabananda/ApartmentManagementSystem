using ApartmentManagementSystem.Application.Interfaces.Maintenance;
using ApartmentManagementSystem.Domain.Entities;

using ApartmentManagementSystem.Application.Features.Home.DTOs;

namespace ApartmentManagementSystem.Application.Features.Maintenance.Services;

public sealed class MaintenanceService(IMaintenanceTicketRepository tickets) : IMaintenanceService
{
    public Task<IReadOnlyList<MaintenanceTicket>> GetForBuildingAsync(Guid buildingId, string? status, CancellationToken cancellationToken = default) =>
        tickets.GetByBuildingAsync(buildingId, status, cancellationToken);

    public Task CreateAsync(MaintenanceTicket ticket, Guid buildingId, CancellationToken cancellationToken = default)
    {
        ticket.BuildingId = buildingId;
        ticket.Status = "Open";
        ticket.CreatedAt = DateTime.UtcNow;
        return tickets.AddAsync(ticket, cancellationToken);
    }

    public async Task<MaintenanceTicket?> AdvanceAsync(Guid id, Guid buildingId, CancellationToken cancellationToken = default)
    {
        var ticket = await tickets.GetAsync(id, buildingId, cancellationToken);
        if (ticket is null) return null;

        ticket.Status = ticket.Status switch
        {
            "Open" => "InProgress",
            "InProgress" => "Closed",
            _ => "Closed"
        };
        if (ticket.Status == "Closed") ticket.ClosedAt = DateTime.UtcNow;

        await tickets.SaveChangesAsync(cancellationToken);
        return ticket;
    }
}
