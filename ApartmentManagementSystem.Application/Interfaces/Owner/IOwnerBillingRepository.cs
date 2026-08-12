using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Application.Features.Home.DTOs;
using ApartmentManagementSystem.Application.Features.Owner.DTOs;
using ApartmentManagementSystem.Application.Features.Tenancy.DTOs;

namespace ApartmentManagementSystem.Application.Interfaces.Owner;

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
