using AMS.Application.Interfaces.Expenses;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Expenses.Queries;

public record GetExpensePaymentsForBuildingQuery(Guid BuildingId) : IRequest<IReadOnlyList<ExpensePayment>>;

public class GetExpensePaymentsForBuildingQueryHandler(IExpensePaymentRepository payments)
    : IRequestHandler<GetExpensePaymentsForBuildingQuery, IReadOnlyList<ExpensePayment>>
{
    public Task<IReadOnlyList<ExpensePayment>> Handle(GetExpensePaymentsForBuildingQuery request, CancellationToken cancellationToken = default)
        => payments.GetByBuildingAsync(request.BuildingId, cancellationToken);
}
