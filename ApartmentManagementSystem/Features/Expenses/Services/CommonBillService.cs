using ApartmentManagementSystem.Features.Expenses.Repositories;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;

namespace ApartmentManagementSystem.Features.Expenses.Services;

public sealed class CommonBillService(ICommonBillRepository bills) : ICommonBillService
{
    public Task<IReadOnlyList<CommonBill>> GetForBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default) => bills.GetByBuildingAsync(buildingId, cancellationToken);
    public Task<CommonBill?> GetAsync(Guid id, bool includeBuilding = false, CancellationToken cancellationToken = default) => bills.GetAsync(id, includeBuilding, cancellationToken);
    public Task<bool> HasPaymentsAsync(Guid billId, CancellationToken cancellationToken = default) => bills.HasPaymentsAsync(billId, cancellationToken);

    public async Task CreateAsync(CommonBill bill, CancellationToken cancellationToken = default)
    {
        bill.BillDate = DateTime.Today;
        await bills.AddAsync(bill, cancellationToken);

        var totalFlats = await bills.CountOwnerFlatsAsync(bill.BuildingId, cancellationToken);
        if (totalFlats == 0) return;

        var amountPerFlat = bill.TotalAmount / totalFlats;
        foreach (var owner in await bills.GetBuildingOwnersAsync(bill.BuildingId, cancellationToken))
        {
            // Deliberately not constrained to the building: this preserves the existing allocation calculation.
            var ownerFlatCount = await bills.CountOwnerFlatsAsync(owner.Id, cancellationToken);
            await bills.AddAllocationAsync(new ExpenseAllocation { CommonBillId = bill.Id, OwnerId = owner.Id, AmountDue = amountPerFlat * ownerFlatCount }, cancellationToken);
        }
        await bills.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CommonBill bill, CommonBill input, CancellationToken cancellationToken = default)
    {
        bill.Name = input.Name;
        bill.TotalAmount = input.TotalAmount;
        bill.Notes = input.Notes;
        await bills.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteAsync(CommonBill bill, CancellationToken cancellationToken = default) => bills.DeleteAsync(bill, cancellationToken);
}
