using AMS.Application.Mediator;
using AMS.Application.Interfaces.Owner;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Owner.Commands;

public record FullPayOwnerBillCommand(string OwnerId, Guid CommonBillId, Guid? RestrictToBuildingId) 
    : IRequest<(bool success, string message, List<ExpenseAllocationPayment> payments)>;

public class FullPayOwnerBillCommandHandler(IOwnerBillingRepository repository)
    : IRequestHandler<FullPayOwnerBillCommand, (bool success, string message, List<ExpenseAllocationPayment> payments)>
{
    public async Task<(bool success, string message, List<ExpenseAllocationPayment> payments)> Handle(FullPayOwnerBillCommand request, CancellationToken cancellationToken = default)
    {
        var created = await repository.RecordFullPayAsync(request.OwnerId, request.CommonBillId, request.RestrictToBuildingId, cancellationToken);
        if (created.Count == 0) return (false, "This bill has no due amount or allocations not found.", []);
        return (true, "Bill fully paid.", created);
    }
}
