using AMS.Application.Mediator;
using AMS.Application.Interfaces.Expenses;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Expenses.Commands;

public record UpdateCommonBillCommand(CommonBill Bill) : IRequest;

public class UpdateCommonBillCommandHandler(ICommonBillRepository bills)
    : IRequestHandler<UpdateCommonBillCommand>
{
    public async Task Handle(UpdateCommonBillCommand request, CancellationToken cancellationToken = default)
    {
        await bills.SaveChangesAsync(cancellationToken);
    }
}
