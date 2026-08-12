using AMS.Application.Interfaces.Expenses;
using AMS.Application.Mediator;

namespace AMS.Application.Features.Expenses.Queries;

public record CheckCommonBillExistsQuery(Guid Id) : IRequest<bool>;

public class CheckCommonBillExistsQueryHandler(ICommonBillRepository bills)
    : IRequestHandler<CheckCommonBillExistsQuery, bool>
{
    public async Task<bool> Handle(CheckCommonBillExistsQuery request, CancellationToken cancellationToken = default)
    {
        var bill = await bills.GetAsync(request.Id, false, cancellationToken);
        return bill != null;
    }
}
