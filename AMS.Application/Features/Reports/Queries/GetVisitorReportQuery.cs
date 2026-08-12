using AMS.Application.Features.Reports.DTOs;
using AMS.Application.Interfaces.Reports;
using AMS.Application.Mediator;

namespace AMS.Application.Features.Reports.Queries;

public record GetVisitorReportQuery(Guid BuildingId, string BuildingName, DateRangeFilter Filter) : IRequest<VisitorReportViewModel>;

public class GetVisitorReportQueryHandler(IPresidentVisitorReportRepository reports)
    : IRequestHandler<GetVisitorReportQuery, VisitorReportViewModel>
{
    public async Task<VisitorReportViewModel> Handle(GetVisitorReportQuery request, CancellationToken cancellationToken = default)
    {
        var (start, end) = request.Filter.ToBoundsOrDefault(30);
        var entries = await reports.GetAsync(request.BuildingId, start, end, cancellationToken);
        return new VisitorReportViewModel { BuildingName = request.BuildingName, Filter = request.Filter, TotalEntries = entries.Count, ByCategory = entries.GroupBy(entry => entry.EntryType).ToDictionary(group => group.Key.ToString(), group => group.Count()), DailyCounts = entries.GroupBy(entry => entry.EntryTime.Date).OrderBy(group => group.Key).Select(group => (group.Key, group.Count())).ToList() };
    }
}
