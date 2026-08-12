using AMS.Application.Mediator;
using AMS.Application.Interfaces.Owner;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Owner.Queries;

public record GetOwnerReceiptQuery(Guid PaymentId) : IRequest<(ExpenseAllocationPayment? payment, Guid? buildingId)>;

public class GetOwnerReceiptQueryHandler(IOwnerBillingRepository repository)
    : IRequestHandler<GetOwnerReceiptQuery, (ExpenseAllocationPayment? payment, Guid? buildingId)>
{
    public Task<(ExpenseAllocationPayment? payment, Guid? buildingId)> Handle(GetOwnerReceiptQuery request, CancellationToken cancellationToken = default)
        => repository.GetReceiptDataAsync(request.PaymentId, cancellationToken);
}
