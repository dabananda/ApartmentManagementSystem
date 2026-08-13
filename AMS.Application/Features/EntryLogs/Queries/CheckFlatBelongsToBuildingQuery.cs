using AMS.Application.Interfaces.EntryLogs;
using AMS.Application.Mediator;

namespace AMS.Application.Features.EntryLogs.Queries;

public record CheckFlatBelongsToBuildingQuery(Guid FlatId, Guid BuildingId) : IRequest<bool>;

public class CheckFlatBelongsToBuildingQueryHandler(IEntryLogRepository entries)
    : IRequestHandler<CheckFlatBelongsToBuildingQuery, bool>
{
    public Task<bool> Handle(CheckFlatBelongsToBuildingQuery request, CancellationToken cancellationToken = default)
        => entries.FlatBelongsToBuildingAsync(request.FlatId, request.BuildingId, cancellationToken);
}
