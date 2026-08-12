using ApartmentManagementSystem.Application.Interfaces.Announcements;
using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Application.Features.Home.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Infrastructure.Repositories.Announcements;

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
