using AMS.Application.Features.Reports.DTOs;

namespace AMS.Application.Features.Reports.Services;

public interface IPresidentFinancialReportService
{
    Task<FinancialReportViewModel> GetAsync(Guid buildingId, string buildingName, DateRangeFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinancialCsvRow>> GetCsvAsync(Guid buildingId, DateRangeFilter filter, CancellationToken cancellationToken = default);
}
public sealed record FinancialCsvRow(DateTime BillDate, string Title, decimal TotalAmount, decimal Collected);
