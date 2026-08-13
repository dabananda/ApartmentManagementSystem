using AMS.Application.Features.Owner.DTOs;
using AMS.Application.Interfaces.Owner;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Owner.Commands;

public record PayOwnerBillCommand(string OwnerId, Guid CommonBillId, RecordOwnerPaymentVM Vm, Guid? RestrictToBuildingId)
    : IRequest<(bool success, string message, IEnumerable<ExpenseAllocationPayment> payments)>;

public class PayOwnerBillCommandHandler(IOwnerBillingRepository repository)
    : IRequestHandler<PayOwnerBillCommand, (bool success, string message, IEnumerable<ExpenseAllocationPayment> payments)>
{
    public async Task<(bool success, string message, IEnumerable<ExpenseAllocationPayment> payments)> Handle(PayOwnerBillCommand request, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.Vm.IdempotencyKey))
        {
            var exists = await repository.IdempotencyKeyExistsAsync(request.Vm.IdempotencyKey, cancellationToken);
            if (exists) return (true, "Payment recorded.", []);
        }

        var allocs = await repository.GetAllocationsForPayAsync(request.OwnerId, request.CommonBillId, request.RestrictToBuildingId, cancellationToken);
        if (allocs.Any() == false) return (false, "No allocation found for this owner & bill.", []);

        var totalDueNow = 0m;
        foreach (var a in allocs)
        {
            var paid = await repository.GetPaidForAllocationAsync(a.Id, cancellationToken);
            totalDueNow += Math.Max(0, a.AmountDue - paid);
        }

        if (totalDueNow <= 0) return (false, "No due for this owner on the selected bill.", []);

        var created = await repository.RecordPayAsync(request.OwnerId, request.CommonBillId, request.Vm, request.RestrictToBuildingId, cancellationToken);
        if (created.Any() == false) return (false, "Failed to record payment.", []);

        var msg = request.Vm.Amount > totalDueNow ? $"Payment recorded (clamped to {totalDueNow:C})." : "Payment recorded.";
        return (true, msg, created);
    }
}


