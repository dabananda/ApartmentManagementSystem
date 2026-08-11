namespace ApartmentManagementSystem.Features.Expenses.Models;

public sealed record OutstandingCommonBill(Guid Id, string Name, decimal Outstanding);
