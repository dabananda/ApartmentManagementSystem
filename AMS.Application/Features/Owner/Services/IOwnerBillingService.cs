using AMS.Domain.Entities;

using AMS.Application.Features.Home.DTOs;
using AMS.Application.Features.Owner.DTOs;
using AMS.Application.Features.Tenancy.DTOs;

namespace AMS.Application.Features.Owner.Services;

public interface IOwnerBillingService
{
    Task<IReadOnlyList<OwnerBillingRow>> GetIndexRowsAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<OwnerBillsPage?> GetBillsPageAsync(string ownerId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default);

    // Receipt fetching
    Task<(ExpenseAllocationPayment? payment, Guid? buildingId)> GetReceiptDataAsync(Guid paymentId, CancellationToken cancellationToken = default);

    // Payments
    Task<(bool success, string message, List<ExpenseAllocationPayment> payments)> PayAsync(string ownerId, Guid commonBillId, RecordOwnerPaymentVM vm, Guid? restrictToBuildingId, CancellationToken cancellationToken = default);
    Task<(bool success, string message, List<ExpenseAllocationPayment> payments)> PayAllAsync(string ownerId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default);
    Task<(bool success, string message, List<ExpenseAllocationPayment> payments)> FullPayAsync(string ownerId, Guid commonBillId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default);
}
