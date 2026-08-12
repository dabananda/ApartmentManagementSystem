using AMS.Application.Mediator;
using AMS.Application.Interfaces.Buildings;

namespace AMS.Application.Features.Buildings.Queries;

public class CheckBuildingCodeExistsQuery : IRequest<bool>
{
    public string Code { get; set; } = default!;
}

public class CheckBuildingCodeExistsQueryHandler : IRequestHandler<CheckBuildingCodeExistsQuery, bool>
{
    private readonly IBuildingRepository _buildingRepository;

    public CheckBuildingCodeExistsQueryHandler(IBuildingRepository buildingRepository)
    {
        _buildingRepository = buildingRepository;
    }

    public Task<bool> Handle(CheckBuildingCodeExistsQuery request, CancellationToken cancellationToken = default)
    {
        return _buildingRepository.CodeExistsAsync(request.Code, cancellationToken);
    }
}
