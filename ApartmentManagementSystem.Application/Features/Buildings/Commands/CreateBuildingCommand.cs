using ApartmentManagementSystem.Application.Mediator;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Application.Interfaces.Buildings;

namespace ApartmentManagementSystem.Application.Features.Buildings.Commands;

public class CreateBuildingCommand : IRequest<Guid>
{
    public Building Building { get; set; } = default!;
}

public class CreateBuildingCommandHandler : IRequestHandler<CreateBuildingCommand, Guid>
{
    private readonly IBuildingRepository _buildingRepository;

    public CreateBuildingCommandHandler(IBuildingRepository buildingRepository)
    {
        _buildingRepository = buildingRepository;
    }

    public async Task<Guid> Handle(CreateBuildingCommand request, CancellationToken cancellationToken = default)
    {
        await _buildingRepository.AddAsync(request.Building, cancellationToken);
        return request.Building.Id;
    }
}
