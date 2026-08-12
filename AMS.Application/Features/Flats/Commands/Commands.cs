using AMS.Application.Mediator;
using AMS.Domain.Entities;
using AMS.Application.Interfaces.Flats;
using System.Threading;
using System.Threading.Tasks;

namespace AMS.Application.Features.Flats.Commands
{
    public record CreateFlatCommand(Flat Flat) : IRequest;
    public class CreateFlatCommandHandler(IFlatRepository flats) : IRequestHandler<CreateFlatCommand>
    {
        public Task Handle(CreateFlatCommand request, CancellationToken cancellationToken)
        {
            request.Flat.CreatedAt = DateTime.UtcNow;
            return flats.AddAsync(request.Flat, cancellationToken);
        }
    }

    public record AssignOwnerCommand(Flat Flat, string? OwnerId) : IRequest;
    public class AssignOwnerCommandHandler(IFlatRepository flats) : IRequestHandler<AssignOwnerCommand>
    {
        public async Task Handle(AssignOwnerCommand request, CancellationToken cancellationToken)
        {
            request.Flat.AssignOwner(request.OwnerId);
            await flats.SaveChangesAsync(cancellationToken);
        }
    }

    public record DeleteFlatCommand(Flat Flat) : IRequest;
    public class DeleteFlatCommandHandler(IFlatRepository flats) : IRequestHandler<DeleteFlatCommand>
    {
        public Task Handle(DeleteFlatCommand request, CancellationToken cancellationToken)
        {
            flats.Remove(request.Flat);
            return flats.SaveChangesAsync(cancellationToken);
        }
    }
}
