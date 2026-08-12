using AMS.Application.Mediator;
using AMS.Application.Interfaces.TenantBilling;

namespace AMS.Application.Features.TenantBilling.Commands;

public record EnsureCurrentMonthTenantBillsCommand(string TenantUserId) : IRequest;

public class EnsureCurrentMonthTenantBillsCommandHandler(ITenantRentRepository repository)
    : IRequestHandler<EnsureCurrentMonthTenantBillsCommand>
{
    public Task Handle(EnsureCurrentMonthTenantBillsCommand request, CancellationToken cancellationToken = default)
        => repository.EnsureCurrentMonthBillsForTenantAsync(request.TenantUserId, cancellationToken);
}
