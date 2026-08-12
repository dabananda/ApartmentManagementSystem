using ApartmentManagementSystem.Application.Mediator;
using ApartmentManagementSystem.Application.Interfaces.Buildings;

namespace ApartmentManagementSystem.Application.Features.Buildings.Queries;

public class CheckBuildingExistsQuery : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class CheckBuildingExistsQueryHandler : IRequestHandler<CheckBuildingExistsQuery, bool>
{
    private readonly IBuildingRepository _buildingRepository;

    public CheckBuildingExistsQueryHandler(IBuildingRepository buildingRepository)
    {
        _buildingRepository = buildingRepository;
    }

    public Task<bool> Handle(CheckBuildingExistsQuery request, CancellationToken cancellationToken = default)
    {
        return _buildingRepository.ExistsAsync(request.Id, cancellationToken);
    }
}
