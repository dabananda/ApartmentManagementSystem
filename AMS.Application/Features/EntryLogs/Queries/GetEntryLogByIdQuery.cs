using AMS.Application.Mediator;
using AMS.Application.Interfaces.EntryLogs;
using AMS.Domain.Entities;

namespace AMS.Application.Features.EntryLogs.Queries;

public record GetEntryLogByIdQuery(Guid Id, bool IncludeReferences = false) : IRequest<EntryLog?>;

public class GetEntryLogByIdQueryHandler(IEntryLogRepository entries)
    : IRequestHandler<GetEntryLogByIdQuery, EntryLog?>
{
    public Task<EntryLog?> Handle(GetEntryLogByIdQuery request, CancellationToken cancellationToken = default)
        => entries.GetAsync(request.Id, request.IncludeReferences, cancellationToken);
}
