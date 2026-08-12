namespace AMS.Application.Features.Expenses.DTOs;

public sealed record OutstandingCommonBill(Guid Id, string Name, decimal Outstanding);
