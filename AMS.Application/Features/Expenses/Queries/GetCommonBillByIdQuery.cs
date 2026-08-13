using AMS.Application.Interfaces.Expenses;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Expenses.Queries;

public record GetCommonBillByIdQuery(Guid Id, bool IncludeBuilding = false) : IRequest<CommonBill?>;

public class GetCommonBillByIdQueryHandler(ICommonBillRepository bills)
    : IRequestHandler<GetCommonBillByIdQuery, CommonBill?>
{
    public Task<CommonBill?> Handle(GetCommonBillByIdQuery request, CancellationToken cancellationToken = default)
        => bills.GetAsync(request.Id, request.IncludeBuilding, cancellationToken);
}
