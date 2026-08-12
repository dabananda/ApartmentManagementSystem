using AMS.Application.Mediator;
using AMS.Application.Interfaces.Reports;
using AMS.Application.Features.Reports.DTOs;
using AMS.Application.Features.Home.DTOs;

namespace AMS.Application.Features.Reports.Queries;

public record GetMaintenanceReportQuery(Guid BuildingId, string BuildingName, DateRangeFilter Filter) : IRequest<MaintenanceReportViewModel>;

public class GetMaintenanceReportQueryHandler(IMaintenanceReportRepository reports)
    : IRequestHandler<GetMaintenanceReportQuery, MaintenanceReportViewModel>
{
    public async Task<MaintenanceReportViewModel> Handle(GetMaintenanceReportQuery request, CancellationToken cancellationToken = default)
    {
        var (start, endExclusive) = request.Filter.ToBoundsOrDefault(90);
        var summary = await reports.GetSummaryAsync(request.BuildingId, start, endExclusive, cancellationToken);

        return new MaintenanceReportViewModel
        {
            BuildingName = request.BuildingName,
            Filter = request.Filter,
            OpenCount = summary.Open,
            InProgressCount = summary.InProgress,
            ClosedCount = summary.Closed,
            AvgResolutionHours = summary.AvgResolutionHours,
            NewlyCreated = summary.CreatedInRange,
            ClosedInRange = summary.ClosedInRange
        };
    }
}
