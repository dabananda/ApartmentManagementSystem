using AMS.Application.Interfaces.Flats;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Flats.Commands;

public record AssignOwnerCommand(Flat Flat, string? OwnerId) : IRequest;

public class AssignOwnerCommandHandler(IFlatRepository flats) : IRequestHandler<AssignOwnerCommand>
{
    public async Task Handle(AssignOwnerCommand request, CancellationToken cancellationToken)
    {
        request.Flat.AssignOwner(request.OwnerId);
        await flats.SaveChangesAsync(cancellationToken);
    }
}
