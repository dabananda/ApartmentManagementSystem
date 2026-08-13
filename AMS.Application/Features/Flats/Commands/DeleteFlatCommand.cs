using AMS.Application.Interfaces.Flats;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Flats.Commands;

public record DeleteFlatCommand(Flat Flat) : IRequest;

public class DeleteFlatCommandHandler(IFlatRepository flats) : IRequestHandler<DeleteFlatCommand>
{
    public Task Handle(DeleteFlatCommand request, CancellationToken cancellationToken)
    {
        flats.Remove(request.Flat);
        return flats.SaveChangesAsync(cancellationToken);
    }
}
