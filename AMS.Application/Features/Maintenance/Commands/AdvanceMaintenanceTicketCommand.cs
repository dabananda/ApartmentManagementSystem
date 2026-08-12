using AMS.Application.Interfaces.Maintenance;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Maintenance.Commands;

public record AdvanceMaintenanceTicketCommand(Guid TicketId, Guid BuildingId) : IRequest<MaintenanceTicket?>;

public class AdvanceMaintenanceTicketCommandHandler(IMaintenanceTicketRepository tickets)
    : IRequestHandler<AdvanceMaintenanceTicketCommand, MaintenanceTicket?>
{
    public async Task<MaintenanceTicket?> Handle(AdvanceMaintenanceTicketCommand request, CancellationToken cancellationToken = default)
    {
        var ticket = await tickets.GetAsync(request.TicketId, request.BuildingId, cancellationToken);
        if (ticket is null) return null;

        ticket.Status = ticket.Status switch
        {
            "Open" => "InProgress",
            "InProgress" => "Closed",
            _ => "Closed"
        };

        if (ticket.Status == "Closed")
            ticket.ClosedAt = DateTime.UtcNow;

        await tickets.SaveChangesAsync(cancellationToken);
        return ticket;
    }
}
