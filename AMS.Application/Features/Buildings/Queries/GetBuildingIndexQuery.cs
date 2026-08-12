using AMS.Application.Mediator;
using AMS.Application.Interfaces.Buildings;
using AMS.Application.Features.Buildings.DTOs;

namespace AMS.Application.Features.Buildings.Queries;

public class GetBuildingIndexQuery : IRequest<BuildingIndexPageViewModel>
{
    public BuildingIndexFilterViewModel Filter { get; set; } = default!;
}

public class GetBuildingIndexQueryHandler : IRequestHandler<GetBuildingIndexQuery, BuildingIndexPageViewModel>
{
    private readonly IBuildingRepository _buildingRepository;

    public GetBuildingIndexQueryHandler(IBuildingRepository buildingRepository)
    {
        _buildingRepository = buildingRepository;
    }

    public Task<BuildingIndexPageViewModel> Handle(GetBuildingIndexQuery request, CancellationToken cancellationToken = default)
    {
        return _buildingRepository.GetIndexAsync(request.Filter, cancellationToken);
    }
}
