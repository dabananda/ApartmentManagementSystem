using AMS.Application.Interfaces.Expenses;
using AMS.Infrastructure.Data;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using AMS.Application.Features.Home.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AMS.Infrastructure.Repositories.Expenses;

public sealed class ExpenseAllocationRepository(ApplicationDbContext context) : IExpenseAllocationRepository
{
    public Task<CommonBill?> GetCommonBillAsync(Guid commonBillId, CancellationToken cancellationToken = default) =>
        context.CommonBills.Include(bill => bill.Building).FirstOrDefaultAsync(bill => bill.Id == commonBillId, cancellationToken);

    public async Task<IReadOnlyList<ExpenseAllocation>> GetAllocationsAsync(Guid commonBillId, CancellationToken cancellationToken = default) =>
        await context.ExpenseAllocations.Include(allocation => allocation.Owner)
            .Where(allocation => allocation.CommonBillId == commonBillId).ToListAsync(cancellationToken);
}
