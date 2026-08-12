using AMS.Application.Mediator;
using AMS.Application.Interfaces.Tenancy;
using AMS.Application.Features.Tenancy.DTOs;

namespace AMS.Application.Features.Tenancy.Queries;

public record GetAssignmentFormDataQuery(string? OwnerId) : IRequest<AssignTenantVM>;

public class GetAssignmentFormDataQueryHandler(ITenantAssignmentRepository repo)
    : IRequestHandler<GetAssignmentFormDataQuery, AssignTenantVM>
{
    public async Task<AssignTenantVM> Handle(GetAssignmentFormDataQuery request, CancellationToken cancellationToken = default)
        => new() { Flats = (await repo.GetFlatsAsync(request.OwnerId)).ToList(), Tenants = (await repo.GetAvailableTenantsAsync()).ToList() };
}
