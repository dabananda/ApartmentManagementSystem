using AMS.Application.Mediator;
using AMS.Application.Interfaces.EntryLogs;
using AMS.Domain.Entities;

namespace AMS.Application.Features.EntryLogs.Commands;

public record CreateEntryLogCommand(EntryLog EntryLog) : IRequest;

public class CreateEntryLogCommandHandler(IEntryLogRepository entries)
    : IRequestHandler<CreateEntryLogCommand>
{
    public Task Handle(CreateEntryLogCommand request, CancellationToken cancellationToken = default)
    {
        request.EntryLog.Id = Guid.NewGuid();
        return entries.AddAsync(request.EntryLog, cancellationToken);
    }
}
