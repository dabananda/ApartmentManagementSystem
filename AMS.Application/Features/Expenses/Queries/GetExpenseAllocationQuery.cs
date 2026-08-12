using AMS.Application.Mediator;
using AMS.Application.Interfaces.Expenses;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Expenses.Queries;

public record GetExpenseAllocationQuery(Guid CommonBillId) : IRequest<(CommonBill? CommonBill, IReadOnlyList<ExpenseAllocation> Allocations)>;

public class GetExpenseAllocationQueryHandler(IExpenseAllocationRepository allocations)
    : IRequestHandler<GetExpenseAllocationQuery, (CommonBill? CommonBill, IReadOnlyList<ExpenseAllocation> Allocations)>
{
    public async Task<(CommonBill? CommonBill, IReadOnlyList<ExpenseAllocation> Allocations)> Handle(GetExpenseAllocationQuery request, CancellationToken cancellationToken = default)
    {
        var bill = await allocations.GetCommonBillAsync(request.CommonBillId, cancellationToken);
        if (bill is null) return (null, Array.Empty<ExpenseAllocation>());
        return (bill, await allocations.GetAllocationsAsync(request.CommonBillId, cancellationToken));
    }
}
