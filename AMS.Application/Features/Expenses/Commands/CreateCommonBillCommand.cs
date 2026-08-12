using AMS.Application.Interfaces.Expenses;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Expenses.Commands;

public record CreateCommonBillCommand(CommonBill Bill) : IRequest;

public class CreateCommonBillCommandHandler(ICommonBillRepository bills)
    : IRequestHandler<CreateCommonBillCommand>
{
    public async Task Handle(CreateCommonBillCommand request, CancellationToken cancellationToken = default)
    {
        request.Bill.BillDate = DateTime.Today;
        await bills.AddAsync(request.Bill, cancellationToken);

        var totalFlats = await bills.CountOwnerFlatsAsync(request.Bill.BuildingId, cancellationToken);
        if (totalFlats == 0) return;

        var amountPerFlat = request.Bill.TotalAmount / totalFlats;
        foreach (var owner in await bills.GetBuildingOwnersAsync(request.Bill.BuildingId, cancellationToken))
        {
            var ownerFlatCount = await bills.CountOwnerFlatsAsync(owner.Id, cancellationToken);
            await bills.AddAllocationAsync(new ExpenseAllocation { CommonBillId = request.Bill.Id, OwnerId = owner.Id, AmountDue = amountPerFlat * ownerFlatCount }, cancellationToken);
        }
        await bills.SaveChangesAsync(cancellationToken);
    }
}
