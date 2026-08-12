using AMS.Application.Features.Reports.DTOs;
using AMS.Application.Interfaces.Reports;
using AMS.Application.Mediator;

namespace AMS.Application.Features.Reports.Queries;

public record GetFinancialReportQuery(Guid BuildingId, string BuildingName, DateRangeFilter Filter) : IRequest<FinancialReportViewModel>;

public class GetFinancialReportQueryHandler(IPresidentFinancialReportRepository reports)
    : IRequestHandler<GetFinancialReportQuery, FinancialReportViewModel>
{
    public async Task<FinancialReportViewModel> Handle(GetFinancialReportQuery request, CancellationToken cancellationToken = default)
    {
        var (start, endExclusive) = request.Filter.ToBoundsOrDefault(60);
        var bills = await reports.GetBillsAsync(request.BuildingId, start, endExclusive, true, cancellationToken);
        var collected = await reports.GetSucceededCollectionsAsync(bills.Select(bill => bill.Id).ToList(), cancellationToken);

        var rows = bills.Select(bill => new FinancialReportRow { CommonBillId = bill.Id, Title = bill.Name, BillDate = bill.BillDate, TotalAmount = bill.TotalAmount, Collected = collected.GetValueOrDefault(bill.Id), Payments = 0m }).ToList();
        return new FinancialReportViewModel { BuildingName = request.BuildingName, Filter = request.Filter, TotalBills = rows.Sum(row => row.TotalAmount), TotalCollected = rows.Sum(row => row.Collected), TotalPayments = await reports.GetPaymentsTotalAsync(request.BuildingId, start, endExclusive, cancellationToken), Rows = rows };
    }
}
