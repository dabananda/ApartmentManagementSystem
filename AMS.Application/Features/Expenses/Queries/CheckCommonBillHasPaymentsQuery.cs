using AMS.Application.Mediator;
using AMS.Application.Interfaces.Expenses;

namespace AMS.Application.Features.Expenses.Queries;

public record CheckCommonBillHasPaymentsQuery(Guid BillId) : IRequest<bool>;

public class CheckCommonBillHasPaymentsQueryHandler(ICommonBillRepository bills)
    : IRequestHandler<CheckCommonBillHasPaymentsQuery, bool>
{
    public Task<bool> Handle(CheckCommonBillHasPaymentsQuery request, CancellationToken cancellationToken = default)
        => bills.HasPaymentsAsync(request.BillId, cancellationToken);
}
