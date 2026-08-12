using AMS.Application.Mediator;
using AMS.Application.Interfaces.Reports;
using AMS.Application.Features.Reports.DTOs;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Reports.Queries;

public record GetVisitorCsvQuery(Guid BuildingId, DateRangeFilter Filter) : IRequest<IReadOnlyList<EntryLog>>;

public class GetVisitorCsvQueryHandler(IPresidentVisitorReportRepository reports)
    : IRequestHandler<GetVisitorCsvQuery, IReadOnlyList<EntryLog>>
{
    public Task<IReadOnlyList<EntryLog>> Handle(GetVisitorCsvQuery request, CancellationToken cancellationToken = default)
    {
        var (start, end) = request.Filter.ToBoundsOrDefault(30); 
        return reports.GetAsync(request.BuildingId, start, end, cancellationToken);
    }
}
