using AMS.Application.Mediator;
using AMS.Application.Interfaces.EntryLogs;
using AMS.Domain.Entities;

namespace AMS.Application.Features.EntryLogs.Queries;

public record GetEntryLogsForBuildingQuery(Guid? BuildingId) : IRequest<IReadOnlyList<EntryLog>>;

public class GetEntryLogsForBuildingQueryHandler(IEntryLogRepository entries)
    : IRequestHandler<GetEntryLogsForBuildingQuery, IReadOnlyList<EntryLog>>
{
    public Task<IReadOnlyList<EntryLog>> Handle(GetEntryLogsForBuildingQuery request, CancellationToken cancellationToken = default)
        => entries.GetForBuildingAsync(request.BuildingId, cancellationToken);
}
