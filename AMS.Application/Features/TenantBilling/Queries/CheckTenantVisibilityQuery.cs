using AMS.Application.Mediator;
using AMS.Application.Interfaces.TenantBilling;

namespace AMS.Application.Features.TenantBilling.Queries;

public record CheckTenantVisibilityQuery(string TenantUserId, string OwnerId) : IRequest<bool>;

public class CheckTenantVisibilityQueryHandler(ITenantRentRepository repository)
    : IRequestHandler<CheckTenantVisibilityQuery, bool>
{
    public Task<bool> Handle(CheckTenantVisibilityQuery request, CancellationToken cancellationToken = default)
        => repository.IsTenantVisibleToOwnerAsync(request.TenantUserId, request.OwnerId, cancellationToken);
}
