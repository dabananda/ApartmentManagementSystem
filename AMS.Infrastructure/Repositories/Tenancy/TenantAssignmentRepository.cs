using AMS.Application.Features.Tenancy.DTOs;
using AMS.Application.Interfaces.Tenancy;
using AMS.Domain.Entities;
using AMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace AMS.Infrastructure.Repositories.Tenancy;

public sealed class TenantAssignmentRepository(ApplicationDbContext db) : ITenantAssignmentRepository
{
    public async Task<IReadOnlyList<Flat>> GetFlatsAsync(string? ownerId) { var q = db.Flats.AsNoTracking().AsQueryable(); if (ownerId != null) q = q.Where(f => f.OwnerId == ownerId); return await q.OrderBy(f => f.FlatNumber).ToListAsync(); }
    public async Task<IReadOnlyList<ApplicationUser>> GetAvailableTenantsAsync() => await db.Users.AsNoTracking().Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Tenant"))).Where(u => !db.TenantAssignments.Any(a => a.TenantUserId == u.Id && a.EndDate == null)).OrderBy(u => u.Fullname ?? u.Email).ToListAsync();
    public Task<Flat?> GetFlatAsync(Guid id) => db.Flats.FindAsync(id).AsTask(); public Task<ApplicationUser?> GetUserAsync(string id) => db.Users.FindAsync(id).AsTask(); public Task<TenantAssignment?> GetActiveForTenantAsync(string id) => db.TenantAssignments.AsNoTracking().FirstOrDefaultAsync(a => a.TenantUserId == id && a.EndDate == null);
    public async Task<IReadOnlyList<MyTenantRow>> GetActiveRowsAsync(string? ownerId) { var q = db.TenantAssignments.AsNoTracking().Include(a => a.Flat).Include(a => a.TenantUser).Where(a => a.EndDate == null); if (ownerId != null) q = q.Where(a => a.Flat!.OwnerId == ownerId); return await q.OrderBy(a => a.Flat!.FlatNumber).Select(a => new MyTenantRow { TenantUserId = a.TenantUserId, TenantName = a.TenantUser!.Fullname ?? a.TenantUser.UserName!, Email = a.TenantUser!.Email!, FlatNumber = a.Flat!.FlatNumber, From = a.StartDate }).ToListAsync(); }
    public async Task ReplaceAsync(Guid flatId, string tenantId) { var today = DateTime.Today; foreach (var a in await db.TenantAssignments.Where(a => a.FlatId == flatId && a.EndDate == null).ToListAsync()) a.EndDate = today.AddDays(-1); await db.TenantAssignments.AddAsync(new TenantAssignment { FlatId = flatId, TenantUserId = tenantId, StartDate = today }); await db.SaveChangesAsync(); }
}
