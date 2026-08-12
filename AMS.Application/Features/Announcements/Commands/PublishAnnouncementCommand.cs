using AMS.Application.Mediator;
using AMS.Application.Interfaces.Announcements;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Announcements.Commands;

public record PublishAnnouncementCommand(Announcement Announcement, Guid BuildingId) : IRequest;

public class PublishAnnouncementCommandHandler(IAnnouncementRepository announcements)
    : IRequestHandler<PublishAnnouncementCommand>
{
    public Task Handle(PublishAnnouncementCommand request, CancellationToken cancellationToken = default)
    {
        request.Announcement.BuildingId = request.BuildingId;
        request.Announcement.CreatedAt = DateTime.UtcNow;
        return announcements.AddAsync(request.Announcement, cancellationToken);
    }
}
