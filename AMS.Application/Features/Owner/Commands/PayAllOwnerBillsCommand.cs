using AMS.Application.Interfaces.Owner;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Owner.Commands;

public record PayAllOwnerBillsCommand(string OwnerId, Guid? RestrictToBuildingId)
    : IRequest<(bool success, string message, List<ExpenseAllocationPayment> payments)>;

public class PayAllOwnerBillsCommandHandler(IOwnerBillingRepository repository)
    : IRequestHandler<PayAllOwnerBillsCommand, (bool success, string message, List<ExpenseAllocationPayment> payments)>
{
    public async Task<(bool success, string message, List<ExpenseAllocationPayment> payments)> Handle(PayAllOwnerBillsCommand request, CancellationToken cancellationToken = default)
    {
        var created = await repository.RecordPayAllAsync(request.OwnerId, request.RestrictToBuildingId, cancellationToken);
        if (created.Count == 0) return (false, "Nothing due to pay or no bills found.", []);

        var total = created.Sum(x => x.Amount);
        return (true, $"All outstanding dues paid ({total:C}).", created);
    }
}
