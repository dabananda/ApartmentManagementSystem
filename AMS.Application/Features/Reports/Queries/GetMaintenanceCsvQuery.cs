using AMS.Application.Features.Reports.DTOs;
using AMS.Application.Interfaces.Reports;
using AMS.Application.Mediator;

namespace AMS.Application.Features.Reports.Queries;

public record GetMaintenanceCsvQuery(Guid BuildingId, DateRangeFilter Filter) : IRequest<IReadOnlyList<MaintenanceCsvRow>>;

public class GetMaintenanceCsvQueryHandler(IMaintenanceReportRepository reports)
    : IRequestHandler<GetMaintenanceCsvQuery, IReadOnlyList<MaintenanceCsvRow>>
{
    public async Task<IReadOnlyList<MaintenanceCsvRow>> Handle(GetMaintenanceCsvQuery request, CancellationToken cancellationToken = default)
    {
        var (start, endExclusive) = request.Filter.ToBoundsOrDefault(90);
        var rows = await reports.GetCsvRowsAsync(request.BuildingId, start, endExclusive, cancellationToken);
        return rows.Select(r => new MaintenanceCsvRow(r.Title, r.Status, r.CreatedAt, r.ClosedAt)).ToList();
    }
}
