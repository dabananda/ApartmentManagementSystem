using AMS.Application.Interfaces.Announcements;
using AMS.Infrastructure.Data;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using AMS.Application.Features.Home.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AMS.Infrastructure.Repositories.Announcements;

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
