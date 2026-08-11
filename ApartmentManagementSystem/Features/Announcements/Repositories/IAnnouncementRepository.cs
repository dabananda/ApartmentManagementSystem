using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;

namespace ApartmentManagementSystem.Features.Announcements.Repositories;

public interface IAnnouncementRepository
{
    Task<IReadOnlyList<Announcement>> GetByBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task AddAsync(Announcement announcement, CancellationToken cancellationToken = default);
}
