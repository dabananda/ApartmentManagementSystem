using ApartmentManagementSystem.Application.Interfaces.Expenses;
using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Application.Features.Home.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Infrastructure.Repositories.Expenses;

public sealed class CommonBillRepository(ApplicationDbContext context) : ICommonBillRepository
{
    public async Task<IReadOnlyList<CommonBill>> GetByBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default) =>
        await context.CommonBills.Where(bill => bill.BuildingId == buildingId).OrderByDescending(bill => bill.BillDate).ToListAsync(cancellationToken);

    public Task<CommonBill?> GetAsync(Guid id, bool includeBuilding = false, CancellationToken cancellationToken = default) =>
        includeBuilding
            ? context.CommonBills.Include(bill => bill.Building).FirstOrDefaultAsync(bill => bill.Id == id, cancellationToken)
            : context.CommonBills.FirstOrDefaultAsync(bill => bill.Id == id, cancellationToken);

    public async Task AddAsync(CommonBill bill, CancellationToken cancellationToken = default)
    {
        await context.CommonBills.AddAsync(bill, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApplicationUser>> GetBuildingOwnersAsync(Guid buildingId, CancellationToken cancellationToken = default) =>
        await context.Flats.Where(flat => flat.BuildingId == buildingId && flat.OwnerId != null)
            .Select(flat => flat.Owner!).Distinct().ToListAsync(cancellationToken);

    public Task<int> CountOwnerFlatsAsync(Guid buildingId, CancellationToken cancellationToken = default) =>
        context.Flats.CountAsync(flat => flat.BuildingId == buildingId && flat.OwnerId != null, cancellationToken);

    public Task<int> CountOwnerFlatsAsync(string ownerId, CancellationToken cancellationToken = default) =>
        context.Flats.CountAsync(flat => flat.OwnerId == ownerId, cancellationToken);

    public Task AddAllocationAsync(ExpenseAllocation allocation, CancellationToken cancellationToken = default) =>
        context.ExpenseAllocations.AddAsync(allocation, cancellationToken).AsTask();

    public Task<bool> HasPaymentsAsync(Guid billId, CancellationToken cancellationToken = default) =>
        context.ExpensePayments.AnyAsync(payment => payment.CommonBillId == billId, cancellationToken);

    public async Task DeleteAsync(CommonBill bill, CancellationToken cancellationToken = default)
    {
        context.ExpenseAllocations.RemoveRange(context.ExpenseAllocations.Where(allocation => allocation.CommonBillId == bill.Id));
        context.CommonBills.Remove(bill);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);
}
