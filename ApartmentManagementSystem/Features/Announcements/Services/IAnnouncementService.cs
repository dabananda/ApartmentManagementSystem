using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;

namespace ApartmentManagementSystem.Features.Announcements.Services;

public interface IAnnouncementService
{
    Task<IReadOnlyList<Announcement>> GetForBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task PublishAsync(Announcement announcement, Guid buildingId, CancellationToken cancellationToken = default);
}
