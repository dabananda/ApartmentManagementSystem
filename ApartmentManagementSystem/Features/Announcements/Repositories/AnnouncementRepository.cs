using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Features.Announcements.Repositories;

public sealed class AnnouncementRepository(ApplicationDbContext context) : IAnnouncementRepository
{
    public async Task<IReadOnlyList<Announcement>> GetByBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default) =>
        await context.Announcements
            .AsNoTracking()
            .Where(announcement => announcement.BuildingId == buildingId)
            .OrderByDescending(announcement => announcement.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Announcement announcement, CancellationToken cancellationToken = default)
    {
        await context.Announcements.AddAsync(announcement, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
