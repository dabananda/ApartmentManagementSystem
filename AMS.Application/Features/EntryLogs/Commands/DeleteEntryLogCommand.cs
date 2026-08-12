using AMS.Application.Mediator;
using AMS.Application.Interfaces.EntryLogs;
using AMS.Domain.Entities;

namespace AMS.Application.Features.EntryLogs.Commands;

public record DeleteEntryLogCommand(EntryLog EntryLog) : IRequest;

public class DeleteEntryLogCommandHandler(IEntryLogRepository entries)
    : IRequestHandler<DeleteEntryLogCommand>
{
    public async Task Handle(DeleteEntryLogCommand request, CancellationToken cancellationToken = default)
    {
        entries.Remove(request.EntryLog);
        await entries.SaveChangesAsync(cancellationToken);
    }
}
