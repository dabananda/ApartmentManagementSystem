using ApartmentManagementSystem.Application.Mediator;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Application.Interfaces.Buildings;

namespace ApartmentManagementSystem.Application.Features.Buildings.Queries;

public class GetBuildingByIdQuery : IRequest<Building?>
{
    public Guid Id { get; set; }
    public bool IncludeFlats { get; set; }
}

public class GetBuildingByIdQueryHandler : IRequestHandler<GetBuildingByIdQuery, Building?>
{
    private readonly IBuildingRepository _buildingRepository;

    public GetBuildingByIdQueryHandler(IBuildingRepository buildingRepository)
    {
        _buildingRepository = buildingRepository;
    }

    public Task<Building?> Handle(GetBuildingByIdQuery request, CancellationToken cancellationToken = default)
    {
        return _buildingRepository.GetAsync(request.Id, request.IncludeFlats, cancellationToken);
    }
}
