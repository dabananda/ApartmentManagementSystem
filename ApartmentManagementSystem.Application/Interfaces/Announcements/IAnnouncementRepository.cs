using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Application.Features.Home.DTOs;

namespace ApartmentManagementSystem.Application.Interfaces.Announcements;

public interface IAnnouncementRepository
{
    Task<IReadOnlyList<Announcement>> GetByBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task AddAsync(Announcement announcement, CancellationToken cancellationToken = default);
}
