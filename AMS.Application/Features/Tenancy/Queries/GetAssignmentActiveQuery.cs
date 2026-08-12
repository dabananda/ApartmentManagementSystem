using AMS.Application.Interfaces.Tenancy;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Tenancy.Queries;

public record GetAssignmentActiveQuery(string Id) : IRequest<TenantAssignment?>;

public class GetAssignmentActiveQueryHandler(ITenantAssignmentRepository repo)
    : IRequestHandler<GetAssignmentActiveQuery, TenantAssignment?>
{
    public Task<TenantAssignment?> Handle(GetAssignmentActiveQuery request, CancellationToken cancellationToken = default)
        => repo.GetActiveForTenantAsync(request.Id);
}
