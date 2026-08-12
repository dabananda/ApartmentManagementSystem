using AMS.Application.Mediator;
using AMS.Application.Interfaces.Buildings;

namespace AMS.Application.Features.Buildings.Queries;

public class CheckBuildingHasBlockingRecordsQuery : IRequest<bool>
{
    public Guid BuildingId { get; set; }
}

public class CheckBuildingHasBlockingRecordsQueryHandler : IRequestHandler<CheckBuildingHasBlockingRecordsQuery, bool>
{
    private readonly IBuildingRepository _buildingRepository;

    public CheckBuildingHasBlockingRecordsQueryHandler(IBuildingRepository buildingRepository)
    {
        _buildingRepository = buildingRepository;
    }

    public Task<bool> Handle(CheckBuildingHasBlockingRecordsQuery request, CancellationToken cancellationToken = default)
    {
        return _buildingRepository.HasBlockingRecordsAsync(request.BuildingId, cancellationToken);
    }
}
