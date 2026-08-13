using AMS.Application.Interfaces.Expenses;
using AMS.Application.Mediator;

namespace AMS.Application.Features.Expenses.Queries;

public record CheckCommonBillHasPaymentsQuery(Guid BillId) : IRequest<bool>;

public class CheckCommonBillHasPaymentsQueryHandler(ICommonBillRepository bills)
    : IRequestHandler<CheckCommonBillHasPaymentsQuery, bool>
{
    public Task<bool> Handle(CheckCommonBillHasPaymentsQuery request, CancellationToken cancellationToken = default)
        => bills.HasPaymentsAsync(request.BillId, cancellationToken);
}
