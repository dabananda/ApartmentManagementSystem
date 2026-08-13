using AMS.Application.Features.Reports.DTOs;
using AMS.Application.Interfaces.Reports;
using AMS.Application.Mediator;

namespace AMS.Application.Features.Reports.Queries;

public record GetOccupancyReportQuery(Guid BuildingId, string BuildingName) : IRequest<OccupancyReportViewModel>;

public class GetOccupancyReportQueryHandler(IPresidentOccupancyReportRepository reports)
    : IRequestHandler<GetOccupancyReportQuery, OccupancyReportViewModel>
{
    public async Task<OccupancyReportViewModel> Handle(GetOccupancyReportQuery request, CancellationToken cancellationToken = default)
    {
        var (total, occupied, owners, tenants) = await reports.GetSummaryAsync(request.BuildingId, cancellationToken);
        return new OccupancyReportViewModel { BuildingName = request.BuildingName, TotalFlats = total, OccupiedFlats = occupied, OwnersCount = owners, TenantsCount = tenants };
    }
}
