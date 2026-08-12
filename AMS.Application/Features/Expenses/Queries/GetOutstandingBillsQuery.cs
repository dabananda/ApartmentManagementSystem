using AMS.Application.Features.Expenses.DTOs;
using AMS.Application.Interfaces.Expenses;
using AMS.Application.Mediator;

namespace AMS.Application.Features.Expenses.Queries;

public record GetOutstandingBillsQuery(Guid BuildingId) : IRequest<IReadOnlyList<OutstandingCommonBill>>;

public class GetOutstandingBillsQueryHandler(IExpensePaymentRepository payments)
    : IRequestHandler<GetOutstandingBillsQuery, IReadOnlyList<OutstandingCommonBill>>
{
    public Task<IReadOnlyList<OutstandingCommonBill>> Handle(GetOutstandingBillsQuery request, CancellationToken cancellationToken = default)
        => payments.GetOutstandingBillsAsync(request.BuildingId, cancellationToken);
}
