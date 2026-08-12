using AMS.Application.Mediator;
using AMS.Application.Interfaces.Tenancy;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Tenancy.Queries;

public record GetAssignmentFlatQuery(Guid FlatId) : IRequest<Flat?>;

public class GetAssignmentFlatQueryHandler(ITenantAssignmentRepository assignmentRepo, ITenantDirectoryRepository directoryRepo, IFlatBillingProfileRepository profileRepo)
    : IRequestHandler<GetAssignmentFlatQuery, Flat?>
{
    public async Task<Flat?> Handle(GetAssignmentFlatQuery request, CancellationToken cancellationToken = default)
    {
        // Try all three repositories since this query is used by multiple features
        var flat = await assignmentRepo.GetFlatAsync(request.FlatId);
        if (flat != null) return flat;
        
        flat = await directoryRepo.GetFlatAsync(request.FlatId, cancellationToken);
        if (flat != null) return flat;
        
        return await profileRepo.GetFlatAsync(request.FlatId, cancellationToken);
    }
}
