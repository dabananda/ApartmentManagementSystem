using AMS.Application.Mediator;
using AMS.Application.Interfaces.TenantPortal;
using AMS.Domain.Entities;

namespace AMS.Application.Features.TenantPortal.Queries;

public record GetTenantActiveAssignmentQuery(string TenantUserId) : IRequest<(TenantAssignment? assignment, ApplicationUser? me)>;

public class GetTenantActiveAssignmentQueryHandler(ITenantPortalRepository repository)
    : IRequestHandler<GetTenantActiveAssignmentQuery, (TenantAssignment? assignment, ApplicationUser? me)>
{
    public Task<(TenantAssignment? assignment, ApplicationUser? me)> Handle(GetTenantActiveAssignmentQuery request, CancellationToken cancellationToken = default)
        => repository.GetActiveAssignmentAsync(request.TenantUserId, cancellationToken);
}
