using AMS.Application.Mediator;
using AMS.Application.Interfaces.Tenancy;

namespace AMS.Application.Features.Tenancy.Queries;

public record CheckTenantExistsQuery(string Id) : IRequest<bool>;

public class CheckTenantExistsQueryHandler(ITenantAssignmentRepository repo)
    : IRequestHandler<CheckTenantExistsQuery, bool>
{
    public async Task<bool> Handle(CheckTenantExistsQuery request, CancellationToken cancellationToken = default)
    {
        var user = await repo.GetUserAsync(request.Id);
        return user != null;
    }
}
