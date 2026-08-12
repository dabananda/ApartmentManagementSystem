using AMS.Application.Interfaces.Maintenance;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Maintenance.Commands;

public record CreateMaintenanceTicketCommand(MaintenanceTicket Ticket, Guid BuildingId) : IRequest;

public class CreateMaintenanceTicketCommandHandler(IMaintenanceTicketRepository tickets)
    : IRequestHandler<CreateMaintenanceTicketCommand>
{
    public Task Handle(CreateMaintenanceTicketCommand request, CancellationToken cancellationToken = default)
    {
        request.Ticket.BuildingId = request.BuildingId;
        request.Ticket.Status = "Open";
        request.Ticket.CreatedAt = DateTime.UtcNow;
        return tickets.AddAsync(request.Ticket, cancellationToken);
    }
}
