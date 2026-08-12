using AMS.Application.Mediator;
using AMS.Application.Interfaces.Tenancy;
using AMS.Application.Features.Flats.DTOs;

namespace AMS.Application.Features.Tenancy.Queries;

public record GetFlatProfileRowsQuery(string? OwnerId) : IRequest<IReadOnlyList<FlatProfileRow>>;

public class GetFlatProfileRowsQueryHandler(IFlatBillingProfileRepository repo)
    : IRequestHandler<GetFlatProfileRowsQuery, IReadOnlyList<FlatProfileRow>>
{
    public Task<IReadOnlyList<FlatProfileRow>> Handle(GetFlatProfileRowsQuery request, CancellationToken cancellationToken = default)
        => repo.GetRowsAsync(request.OwnerId, cancellationToken);
}
