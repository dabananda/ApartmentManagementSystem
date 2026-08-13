using AMS.Application.Interfaces.Flats;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Flats.Queries;

public record GetActiveAssignmentsQuery : IRequest<IReadOnlyList<TenantAssignment>>;

public class GetActiveAssignmentsQueryHandler(IFlatRepository flats)
    : IRequestHandler<GetActiveAssignmentsQuery, IReadOnlyList<TenantAssignment>>
{
    public Task<IReadOnlyList<TenantAssignment>> Handle(GetActiveAssignmentsQuery request, CancellationToken cancellationToken) =>
        flats.GetActiveAssignmentsAsync(cancellationToken);
}
