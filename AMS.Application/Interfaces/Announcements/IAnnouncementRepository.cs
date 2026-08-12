using AMS.Domain.Entities;
using AMS.Application.Features.Home.DTOs;

namespace AMS.Application.Interfaces.Announcements;

public interface IAnnouncementRepository
{
    Task<IReadOnlyList<Announcement>> GetByBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task AddAsync(Announcement announcement, CancellationToken cancellationToken = default);
}
