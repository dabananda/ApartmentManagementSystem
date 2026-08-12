using AMS.Application.Mediator;
using AMS.Application.Interfaces.President;
using AMS.Application.Features.President.DTOs;

namespace AMS.Application.Features.President.Queries;

public record GetPresidentDashboardQuery(Guid BuildingId) : IRequest<PresidentDashboardViewModel>;

public class GetPresidentDashboardQueryHandler(IPresidentDashboardRepository repository)
    : IRequestHandler<GetPresidentDashboardQuery, PresidentDashboardViewModel>
{
    public Task<PresidentDashboardViewModel> Handle(GetPresidentDashboardQuery request, CancellationToken cancellationToken = default)
        => repository.GetAsync(request.BuildingId, cancellationToken);
}
