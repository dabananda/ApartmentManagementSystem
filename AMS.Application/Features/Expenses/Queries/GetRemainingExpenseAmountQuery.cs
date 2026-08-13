using AMS.Application.Interfaces.Expenses;
using AMS.Application.Mediator;

namespace AMS.Application.Features.Expenses.Queries;

public record GetRemainingExpenseAmountQuery(Guid CommonBillId) : IRequest<decimal>;

public class GetRemainingExpenseAmountQueryHandler(IExpensePaymentRepository payments)
    : IRequestHandler<GetRemainingExpenseAmountQuery, decimal>
{
    public async Task<decimal> Handle(GetRemainingExpenseAmountQuery request, CancellationToken cancellationToken = default)
    {
        var paidSoFar = await payments.GetPaidAmountAsync(request.CommonBillId, cancellationToken);
        var bill = await payments.GetBillAsync(request.CommonBillId, cancellationToken);
        return bill!.TotalAmount - paidSoFar;
    }
}
