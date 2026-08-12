using ApartmentManagementSystem.Application.Interfaces.Expenses;
using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Application.Features.Home.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Infrastructure.Repositories.Expenses;

public sealed class ExpenseAllocationRepository(ApplicationDbContext context) : IExpenseAllocationRepository
{
    public Task<CommonBill?> GetCommonBillAsync(Guid commonBillId, CancellationToken cancellationToken = default) =>
        context.CommonBills.Include(bill => bill.Building).FirstOrDefaultAsync(bill => bill.Id == commonBillId, cancellationToken);

    public async Task<IReadOnlyList<ExpenseAllocation>> GetAllocationsAsync(Guid commonBillId, CancellationToken cancellationToken = default) =>
        await context.ExpenseAllocations.Include(allocation => allocation.Owner)
            .Where(allocation => allocation.CommonBillId == commonBillId).ToListAsync(cancellationToken);
}
