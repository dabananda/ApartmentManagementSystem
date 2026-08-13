using AMS.Application.Interfaces.TenantBilling;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.TenantBilling.Commands;

public record FullPayTenantBillCommand(Guid BillId, string? RestrictToOwnerId)
    : IRequest<(bool success, string message, IEnumerable<TenantPayment> payments, string? tenantUserId)>;

public class FullPayTenantBillCommandHandler(ITenantRentRepository repository)
    : IRequestHandler<FullPayTenantBillCommand, (bool success, string message, IEnumerable<TenantPayment> payments, string? tenantUserId)>
{
    public async Task<(bool success, string message, IEnumerable<TenantPayment> payments, string? tenantUserId)> Handle(FullPayTenantBillCommand request, CancellationToken cancellationToken = default)
    {
        var (created, tenantUserId) = await repository.RecordFullPayAsync(request.BillId, request.RestrictToOwnerId, cancellationToken);
        if (created.Any() == false) return (false, "No due on this bill or bill not found.", [], tenantUserId);

        return (true, "Bill fully paid.", created, tenantUserId);
    }
}


