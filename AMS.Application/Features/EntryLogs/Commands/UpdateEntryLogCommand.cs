using AMS.Application.Mediator;
using AMS.Application.Interfaces.EntryLogs;
using AMS.Domain.Entities;

namespace AMS.Application.Features.EntryLogs.Commands;

public record UpdateEntryLogCommand(EntryLog Existing, EntryLog Input) : IRequest;

public class UpdateEntryLogCommandHandler(IEntryLogRepository entries)
    : IRequestHandler<UpdateEntryLogCommand>
{
    public async Task Handle(UpdateEntryLogCommand request, CancellationToken cancellationToken = default)
    {
        request.Existing.Fullname = request.Input.Fullname;
        request.Existing.PhoneNumber = request.Input.PhoneNumber;
        request.Existing.BuildingId = request.Input.BuildingId;
        request.Existing.FlatId = request.Input.FlatId;
        request.Existing.EntryType = request.Input.EntryType;
        request.Existing.NumberOfPerson = request.Input.NumberOfPerson;
        request.Existing.Purpose = request.Input.Purpose;
        request.Existing.EntryTime = request.Input.EntryTime;
        request.Existing.ExitTime = request.Input.ExitTime;
        await entries.SaveChangesAsync(cancellationToken);
    }
}
