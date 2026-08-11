using ApartmentManagementSystem.Features.Announcements.Repositories;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;

namespace ApartmentManagementSystem.Features.Announcements.Services;

public sealed class AnnouncementService(IAnnouncementRepository announcements) : IAnnouncementService
{
    public Task<IReadOnlyList<Announcement>> GetForBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default) =>
        announcements.GetByBuildingAsync(buildingId, cancellationToken);

    public Task PublishAsync(Announcement announcement, Guid buildingId, CancellationToken cancellationToken = default)
    {
        announcement.Id = Guid.NewGuid();
        announcement.BuildingId = buildingId;
        announcement.CreatedAt = DateTime.UtcNow;
        return announcements.AddAsync(announcement, cancellationToken);
    }
}
