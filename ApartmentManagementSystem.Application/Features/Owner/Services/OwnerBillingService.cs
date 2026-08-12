using ApartmentManagementSystem.Application.Interfaces.Owner;
using ApartmentManagementSystem.Domain.Entities;

using ApartmentManagementSystem.Application.Features.Home.DTOs;
using ApartmentManagementSystem.Application.Features.Owner.DTOs;
using ApartmentManagementSystem.Application.Features.Tenancy.DTOs;

namespace ApartmentManagementSystem.Application.Features.Owner.Services;

public sealed class OwnerBillingService(IOwnerBillingRepository repository) : IOwnerBillingService
{
    public Task<IReadOnlyList<OwnerBillingRow>> GetIndexRowsAsync(Guid buildingId, CancellationToken cancellationToken = default) =>
        repository.GetIndexRowsAsync(buildingId, cancellationToken);

    public Task<OwnerBillsPage?> GetBillsPageAsync(string ownerId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default) =>
        repository.GetBillsPageAsync(ownerId, restrictToBuildingId, cancellationToken);

    public Task<(ExpenseAllocationPayment? payment, Guid? buildingId)> GetReceiptDataAsync(Guid paymentId, CancellationToken cancellationToken = default) =>
        repository.GetReceiptDataAsync(paymentId, cancellationToken);

    public async Task<(bool success, string message, List<ExpenseAllocationPayment> payments)> PayAsync(string ownerId, Guid commonBillId, RecordOwnerPaymentVM vm, Guid? restrictToBuildingId, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(vm.IdempotencyKey))
        {
            var exists = await repository.IdempotencyKeyExistsAsync(vm.IdempotencyKey, cancellationToken);
            if (exists) return (true, "Payment recorded.", []);
        }

        var allocs = await repository.GetAllocationsForPayAsync(ownerId, commonBillId, restrictToBuildingId, cancellationToken);
        if (allocs.Count == 0) return (false, "No allocation found for this owner & bill.", []);

        var totalDueNow = 0m;
        foreach (var a in allocs)
        {
            var paid = await repository.GetPaidForAllocationAsync(a.Id, cancellationToken);
            totalDueNow += Math.Max(0, a.AmountDue - paid);
        }

        if (totalDueNow <= 0) return (false, "No due for this owner on the selected bill.", []);

        var created = await repository.RecordPayAsync(ownerId, commonBillId, vm, restrictToBuildingId, cancellationToken);
        if (created.Count == 0) return (false, "Failed to record payment.", []);

        var msg = vm.Amount > totalDueNow ? $"Payment recorded (clamped to {totalDueNow:C})." : "Payment recorded.";
        return (true, msg, created);
    }

    public async Task<(bool success, string message, List<ExpenseAllocationPayment> payments)> PayAllAsync(string ownerId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default)
    {
        var created = await repository.RecordPayAllAsync(ownerId, restrictToBuildingId, cancellationToken);
        if (created.Count == 0) return (false, "Nothing due to pay or no bills found.", []);

        var total = created.Sum(x => x.Amount);
        return (true, $"All outstanding dues paid ({total:C}).", created);
    }

    public async Task<(bool success, string message, List<ExpenseAllocationPayment> payments)> FullPayAsync(string ownerId, Guid commonBillId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default)
    {
        var created = await repository.RecordFullPayAsync(ownerId, commonBillId, restrictToBuildingId, cancellationToken);
        if (created.Count == 0) return (false, "This bill has no due amount or allocations not found.", []);
        return (true, "Bill fully paid.", created);
    }
}
