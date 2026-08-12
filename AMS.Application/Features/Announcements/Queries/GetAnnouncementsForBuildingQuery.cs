using AMS.Application.Mediator;
using AMS.Application.Interfaces.Announcements;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Announcements.Queries;

public record GetAnnouncementsForBuildingQuery(Guid BuildingId) : IRequest<IReadOnlyList<Announcement>>;

public class GetAnnouncementsForBuildingQueryHandler(IAnnouncementRepository announcements)
    : IRequestHandler<GetAnnouncementsForBuildingQuery, IReadOnlyList<Announcement>>
{
    public Task<IReadOnlyList<Announcement>> Handle(GetAnnouncementsForBuildingQuery request, CancellationToken cancellationToken = default)
        => announcements.GetByBuildingAsync(request.BuildingId, cancellationToken);
}
