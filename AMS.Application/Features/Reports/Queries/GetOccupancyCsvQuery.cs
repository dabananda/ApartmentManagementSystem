using AMS.Application.Interfaces.Reports;
using AMS.Application.Mediator;

namespace AMS.Application.Features.Reports.Queries;

public record GetOccupancyCsvQuery(Guid BuildingId) : IRequest<IReadOnlyList<OccupancyFlatRow>>;

public class GetOccupancyCsvQueryHandler(IPresidentOccupancyReportRepository reports)
    : IRequestHandler<GetOccupancyCsvQuery, IReadOnlyList<OccupancyFlatRow>>
{
    public Task<IReadOnlyList<OccupancyFlatRow>> Handle(GetOccupancyCsvQuery request, CancellationToken cancellationToken = default)
        => reports.GetFlatsAsync(request.BuildingId, cancellationToken);
}
