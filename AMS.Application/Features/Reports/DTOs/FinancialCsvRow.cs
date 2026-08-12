namespace AMS.Application.Features.Reports.DTOs;

public sealed record FinancialCsvRow(DateTime BillDate, string Title, decimal TotalAmount, decimal Collected);
