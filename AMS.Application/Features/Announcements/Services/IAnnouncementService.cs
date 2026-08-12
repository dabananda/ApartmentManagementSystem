using AMS.Domain.Entities;

using AMS.Application.Features.Home.DTOs;

namespace AMS.Application.Features.Announcements.Services;

public interface IAnnouncementService
{
    Task<IReadOnlyList<Announcement>> GetForBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task PublishAsync(Announcement announcement, Guid buildingId, CancellationToken cancellationToken = default);
}
