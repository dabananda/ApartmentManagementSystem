using AMS.Application.Interfaces.Flats;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Flats.Commands;

public record CreateFlatCommand(Flat Flat) : IRequest;

public class CreateFlatCommandHandler(IFlatRepository flats) : IRequestHandler<CreateFlatCommand>
{
    public Task Handle(CreateFlatCommand request, CancellationToken cancellationToken)
    {
        request.Flat.CreatedAt = DateTime.UtcNow;
        return flats.AddAsync(request.Flat, cancellationToken);
    }
}
