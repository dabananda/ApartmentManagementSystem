using AMS.Application.Interfaces.Expenses;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Expenses.Queries;

public record GetExpensePaymentByIdQuery(Guid Id) : IRequest<ExpensePayment?>;

public class GetExpensePaymentByIdQueryHandler(IExpensePaymentRepository payments)
    : IRequestHandler<GetExpensePaymentByIdQuery, ExpensePayment?>
{
    public Task<ExpensePayment?> Handle(GetExpensePaymentByIdQuery request, CancellationToken cancellationToken = default)
        => payments.GetAsync(request.Id, cancellationToken);
}
