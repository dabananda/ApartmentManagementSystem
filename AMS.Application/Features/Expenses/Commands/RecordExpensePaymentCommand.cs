using AMS.Application.Interfaces.Expenses;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Expenses.Commands;

public record RecordExpensePaymentCommand(ExpensePayment Payment) : IRequest;

public class RecordExpensePaymentCommandHandler(IExpensePaymentRepository payments)
    : IRequestHandler<RecordExpensePaymentCommand>
{
    public Task Handle(RecordExpensePaymentCommand request, CancellationToken cancellationToken = default)
        => payments.AddAsync(request.Payment, cancellationToken);
}
