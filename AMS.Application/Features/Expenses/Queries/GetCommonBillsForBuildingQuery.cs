using AMS.Application.Mediator;
using AMS.Application.Interfaces.Expenses;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Expenses.Queries;

public record GetCommonBillsForBuildingQuery(Guid BuildingId) : IRequest<IReadOnlyList<CommonBill>>;

public class GetCommonBillsForBuildingQueryHandler(ICommonBillRepository bills)
    : IRequestHandler<GetCommonBillsForBuildingQuery, IReadOnlyList<CommonBill>>
{
    public Task<IReadOnlyList<CommonBill>> Handle(GetCommonBillsForBuildingQuery request, CancellationToken cancellationToken = default)
        => bills.GetByBuildingAsync(request.BuildingId, cancellationToken);
}
