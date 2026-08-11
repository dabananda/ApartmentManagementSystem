using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using ApartmentManagementSystem.Features.Owner.ViewModels;
using ApartmentManagementSystem.Features.Tenancy.ViewModels;

namespace ApartmentManagementSystem.Features.Owner.Repositories;

public interface IOwnerBillingRepository
{
    Task<IReadOnlyList<OwnerBillingRow>> GetIndexRowsAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<OwnerBillsPage?> GetBillsPageAsync(string ownerId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default);
    Task<(ExpenseAllocationPayment? payment, Guid? buildingId)> GetReceiptDataAsync(Guid paymentId, CancellationToken cancellationToken = default);

    // Payment workflow
    Task<bool> IdempotencyKeyExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<List<ExpenseAllocation>> GetAllocationsForPayAsync(string ownerId, Guid commonBillId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default);
    Task<decimal> GetPaidForAllocationAsync(Guid allocationId, CancellationToken cancellationToken = default);
    Task<List<ExpenseAllocationPayment>> RecordPayAsync(string ownerId, Guid commonBillId, RecordOwnerPaymentVM vm, Guid? restrictToBuildingId, CancellationToken cancellationToken = default);
    Task<List<ExpenseAllocationPayment>> RecordPayAllAsync(string ownerId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default);
    Task<List<ExpenseAllocationPayment>> RecordFullPayAsync(string ownerId, Guid commonBillId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default);
}
