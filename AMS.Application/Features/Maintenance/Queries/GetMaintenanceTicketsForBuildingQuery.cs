using AMS.Application.Mediator;
using AMS.Application.Interfaces.Maintenance;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Maintenance.Queries;

public record GetMaintenanceTicketsForBuildingQuery(Guid BuildingId, string? Status) : IRequest<IReadOnlyList<MaintenanceTicket>>;

public class GetMaintenanceTicketsForBuildingQueryHandler(IMaintenanceTicketRepository tickets)
    : IRequestHandler<GetMaintenanceTicketsForBuildingQuery, IReadOnlyList<MaintenanceTicket>>
{
    public Task<IReadOnlyList<MaintenanceTicket>> Handle(GetMaintenanceTicketsForBuildingQuery request, CancellationToken cancellationToken = default)
        => tickets.GetByBuildingAsync(request.BuildingId, request.Status, cancellationToken);
}
