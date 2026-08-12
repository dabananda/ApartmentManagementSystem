using AMS.Application.Mediator;
using AMS.Domain.Entities;
using AMS.Application.Interfaces.Buildings;

namespace AMS.Application.Features.Buildings.Commands;

public class UpdateBuildingCommand : IRequest<Guid>
{
    public Building Building { get; set; } = default!;
}

public class UpdateBuildingCommandHandler : IRequestHandler<UpdateBuildingCommand, Guid>
{
    private readonly IBuildingRepository _buildingRepository;

    public UpdateBuildingCommandHandler(IBuildingRepository buildingRepository)
    {
        _buildingRepository = buildingRepository;
    }

    public async Task<Guid> Handle(UpdateBuildingCommand request, CancellationToken cancellationToken = default)
    {
        await _buildingRepository.UpdateAsync(request.Building, cancellationToken);
        return request.Building.Id;
    }
}
