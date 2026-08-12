using AMS.Application.Mediator;
using AMS.Domain.Entities;
using AMS.Application.Interfaces.Buildings;

namespace AMS.Application.Features.Buildings.Commands;

public class DeleteBuildingCommand : IRequest<Guid>
{
    public Building Building { get; set; } = default!;
}

public class DeleteBuildingCommandHandler : IRequestHandler<DeleteBuildingCommand, Guid>
{
    private readonly IBuildingRepository _buildingRepository;

    public DeleteBuildingCommandHandler(IBuildingRepository buildingRepository)
    {
        _buildingRepository = buildingRepository;
    }

    public async Task<Guid> Handle(DeleteBuildingCommand request, CancellationToken cancellationToken = default)
    {
        await _buildingRepository.DeleteAsync(request.Building, cancellationToken);
        return request.Building.Id;
    }
}
