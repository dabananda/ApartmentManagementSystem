using ApartmentManagementSystem.Features.Reports.Repositories;
using ApartmentManagementSystem.Features.Reports.ViewModels;

namespace ApartmentManagementSystem.Features.Reports.Services;
public sealed class PresidentFinancialReportService(IPresidentFinancialReportRepository reports) : IPresidentFinancialReportService
{
    public async Task<FinancialReportViewModel> GetAsync(Guid buildingId, string buildingName, DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var (start, endExclusive) = filter.ToBoundsOrDefault(60); var bills = await reports.GetBillsAsync(buildingId, start, endExclusive, true, cancellationToken); var collected = await reports.GetSucceededCollectionsAsync(bills.Select(bill => bill.Id).ToList(), cancellationToken);
        var rows = bills.Select(bill => new FinancialReportRow { CommonBillId = bill.Id, Title = bill.Name, BillDate = bill.BillDate, TotalAmount = bill.TotalAmount, Collected = collected.GetValueOrDefault(bill.Id), Payments = 0m }).ToList();
        return new FinancialReportViewModel { BuildingName = buildingName, Filter = filter, TotalBills = rows.Sum(row => row.TotalAmount), TotalCollected = rows.Sum(row => row.Collected), TotalPayments = await reports.GetPaymentsTotalAsync(buildingId, start, endExclusive, cancellationToken), Rows = rows };
    }
    public async Task<IReadOnlyList<FinancialCsvRow>> GetCsvAsync(Guid buildingId, DateRangeFilter filter, CancellationToken cancellationToken = default)
    {
        var (start, endExclusive) = filter.ToBoundsOrDefault(60); var bills = await reports.GetBillsAsync(buildingId, start, endExclusive, false, cancellationToken); var collected = await reports.GetPaidAllocationCollectionsAsync(bills.Select(bill => bill.Id).ToList(), cancellationToken);
        return bills.Select(bill => new FinancialCsvRow(bill.BillDate, bill.Name, bill.TotalAmount, collected.GetValueOrDefault(bill.Id))).ToList();
    }
}
