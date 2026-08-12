using AMS.Application.Features.Tenancy.DTOs;
using AMS.Application.Interfaces.Tenancy;
using AMS.Application.Mediator;

namespace AMS.Application.Features.Tenancy.Queries;

public record GetActiveTenantAssignmentsQuery(string? OwnerId) : IRequest<IReadOnlyList<MyTenantRow>>;

public class GetActiveTenantAssignmentsQueryHandler(ITenantAssignmentRepository repo)
    : IRequestHandler<GetActiveTenantAssignmentsQuery, IReadOnlyList<MyTenantRow>>
{
    public Task<IReadOnlyList<MyTenantRow>> Handle(GetActiveTenantAssignmentsQuery request, CancellationToken cancellationToken = default)
        => repo.GetActiveRowsAsync(request.OwnerId);
}
