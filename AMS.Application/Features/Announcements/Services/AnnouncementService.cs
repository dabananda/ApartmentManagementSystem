using AMS.Application.Interfaces.Announcements;
using AMS.Domain.Entities;

using AMS.Application.Features.Home.DTOs;

namespace AMS.Application.Features.Announcements.Services;

public sealed class AnnouncementService(IAnnouncementRepository announcements) : IAnnouncementService
{
    public Task<IReadOnlyList<Announcement>> GetForBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default) =>
        announcements.GetByBuildingAsync(buildingId, cancellationToken);

    public Task PublishAsync(Announcement announcement, Guid buildingId, CancellationToken cancellationToken = default)
    {
        announcement.BuildingId = buildingId;
        announcement.CreatedAt = DateTime.UtcNow;
        return announcements.AddAsync(announcement, cancellationToken);
    }
}
