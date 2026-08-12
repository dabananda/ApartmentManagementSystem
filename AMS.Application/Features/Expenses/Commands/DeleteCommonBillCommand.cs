using AMS.Application.Mediator;
using AMS.Application.Interfaces.Expenses;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Expenses.Commands;

public record DeleteCommonBillCommand(CommonBill Bill) : IRequest;

public class DeleteCommonBillCommandHandler(ICommonBillRepository bills)
    : IRequestHandler<DeleteCommonBillCommand>
{
    public Task Handle(DeleteCommonBillCommand request, CancellationToken cancellationToken = default)
        => bills.DeleteAsync(request.Bill, cancellationToken);
}
