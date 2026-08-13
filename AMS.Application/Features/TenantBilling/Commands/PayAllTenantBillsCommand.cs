using AMS.Application.Interfaces.TenantBilling;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.TenantBilling.Commands;

public record PayAllTenantBillsCommand(string TenantUserId, string? RestrictToOwnerId)
    : IRequest<(bool success, string message, IEnumerable<TenantPayment> payments, string? tenantUserId)>;

public class PayAllTenantBillsCommandHandler(ITenantRentRepository repository)
    : IRequestHandler<PayAllTenantBillsCommand, (bool success, string message, IEnumerable<TenantPayment> payments, string? tenantUserId)>
{
    public async Task<(bool success, string message, IEnumerable<TenantPayment> payments, string? tenantUserId)> Handle(PayAllTenantBillsCommand request, CancellationToken cancellationToken = default)
    {
        var (created, returnedTenantId) = await repository.RecordPayAllAsync(request.TenantUserId, request.RestrictToOwnerId, cancellationToken);
        if (created.Any() == false) return (false, "Nothing due to pay or no bills found.", [], request.TenantUserId);

        return (true, "All outstanding dues paid.", created, request.TenantUserId);
    }
}


