using AMS.Application.Mediator;
using AMS.Application.Interfaces.EntryLogs;
using AMS.Domain.Entities;

namespace AMS.Application.Features.EntryLogs.Queries;

public record GetEntryLogFormDataQuery(Guid? BuildingId) : IRequest<(IReadOnlyList<Building> Buildings, IReadOnlyList<Flat> Flats)>;

public class GetEntryLogFormDataQueryHandler(IEntryLogRepository entries)
    : IRequestHandler<GetEntryLogFormDataQuery, (IReadOnlyList<Building> Buildings, IReadOnlyList<Flat> Flats)>
{
    public async Task<(IReadOnlyList<Building> Buildings, IReadOnlyList<Flat> Flats)> Handle(GetEntryLogFormDataQuery request, CancellationToken cancellationToken = default)
    {
        var buildings = await entries.GetBuildingsAsync(request.BuildingId, cancellationToken);
        var flats = await entries.GetFlatsAsync(request.BuildingId, cancellationToken);
        return (buildings, flats);
    }
}
