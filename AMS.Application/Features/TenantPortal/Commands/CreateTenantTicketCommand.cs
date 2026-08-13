using AMS.Application.Interfaces.TenantPortal;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.TenantPortal.Commands;

public record CreateTenantTicketCommand(MaintenanceTicket Ticket) : IRequest;

public class CreateTenantTicketCommandHandler(ITenantPortalRepository repository)
    : IRequestHandler<CreateTenantTicketCommand>
{
    public Task Handle(CreateTenantTicketCommand request, CancellationToken cancellationToken = default)
        => repository.CreateTicketAsync(request.Ticket, cancellationToken);
}
