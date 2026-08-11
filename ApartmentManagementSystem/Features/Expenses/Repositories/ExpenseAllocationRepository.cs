using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Features.Expenses.Repositories;

public sealed class ExpenseAllocationRepository(ApplicationDbContext context) : IExpenseAllocationRepository
{
    public Task<CommonBill?> GetCommonBillAsync(Guid commonBillId, CancellationToken cancellationToken = default) =>
        context.CommonBills.Include(bill => bill.Building).FirstOrDefaultAsync(bill => bill.Id == commonBillId, cancellationToken);

    public async Task<IReadOnlyList<ExpenseAllocation>> GetAllocationsAsync(Guid commonBillId, CancellationToken cancellationToken = default) =>
        await context.ExpenseAllocations.Include(allocation => allocation.Owner)
            .Where(allocation => allocation.CommonBillId == commonBillId).ToListAsync(cancellationToken);
}
