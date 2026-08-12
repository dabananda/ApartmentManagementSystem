using AMS.Application.Mediator;
using AMS.Domain.Entities;
using AMS.Application.Interfaces.Buildings;

namespace AMS.Application.Features.Buildings.Queries;

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
