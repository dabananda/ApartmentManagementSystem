using AMS.Application.Features.Owner.DTOs;
using AMS.Domain.Entities;

namespace AMS.Application.Interfaces.Owner;

public interface IOwnerBillingRepository
{
    Task<IReadOnlyList<OwnerBillingRow>> GetIndexRowsAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<OwnerBillsPage?> GetBillsPageAsync(string ownerId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default);
    Task<(ExpenseAllocationPayment? payment, Guid? buildingId)> GetReceiptDataAsync(Guid paymentId, CancellationToken cancellationToken = default);

    Task<bool> IdempotencyKeyExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExpenseAllocation>> GetAllocationsForPayAsync(string ownerId, Guid commonBillId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default);
    Task<decimal> GetPaidForAllocationAsync(Guid allocationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExpenseAllocationPayment>> RecordPayAsync(string ownerId, Guid commonBillId, RecordOwnerPaymentVM vm, Guid? restrictToBuildingId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExpenseAllocationPayment>> RecordPayAllAsync(string ownerId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExpenseAllocationPayment>> RecordFullPayAsync(string ownerId, Guid commonBillId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default);
}

