using AMS.Application.Mediator;
using AMS.Application.Interfaces.Reports;
using AMS.Application.Features.Reports.DTOs;
using AMS.Application.Features.Home.DTOs;

namespace AMS.Application.Features.Reports.Queries;

public record GetFinancialCsvQuery(Guid BuildingId, DateRangeFilter Filter) : IRequest<IReadOnlyList<FinancialCsvRow>>;

public class GetFinancialCsvQueryHandler(IPresidentFinancialReportRepository reports)
    : IRequestHandler<GetFinancialCsvQuery, IReadOnlyList<FinancialCsvRow>>
{
    public async Task<IReadOnlyList<FinancialCsvRow>> Handle(GetFinancialCsvQuery request, CancellationToken cancellationToken = default)
    {
        var (start, endExclusive) = request.Filter.ToBoundsOrDefault(60); 
        var bills = await reports.GetBillsAsync(request.BuildingId, start, endExclusive, false, cancellationToken); 
        var collected = await reports.GetPaidAllocationCollectionsAsync(bills.Select(bill => bill.Id).ToList(), cancellationToken);
        return bills.Select(bill => new FinancialCsvRow(bill.BillDate, bill.Name, bill.TotalAmount, collected.GetValueOrDefault(bill.Id))).ToList();
    }
}
