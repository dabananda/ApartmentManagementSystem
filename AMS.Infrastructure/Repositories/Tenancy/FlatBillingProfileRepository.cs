using AMS.Application.Features.Flats.DTOs;
using AMS.Application.Interfaces.Tenancy;
using AMS.Domain.Entities;
using AMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AMS.Infrastructure.Repositories.Tenancy;

public sealed class FlatBillingProfileRepository(ApplicationDbContext context) : IFlatBillingProfileRepository
{
    public async Task<IReadOnlyList<FlatProfileRow>> GetRowsAsync(string? ownerId, CancellationToken cancellationToken = default)
    {
        var flats = context.Flats.AsNoTracking().AsQueryable(); if (ownerId is not null) flats = flats.Where(flat => flat.OwnerId == ownerId);
        return await flats.GroupJoin(context.FlatBillingProfiles, flat => flat.Id, profile => profile.FlatId, (flat, profiles) => new { flat, profile = profiles.FirstOrDefault() })
            .OrderBy(row => row.flat.FlatNumber).Select(row => new FlatProfileRow { FlatId = row.flat.Id, FlatNumber = row.flat.FlatNumber, HasProfile = row.profile != null, Title = row.profile != null ? row.profile.Title : "", Amount = row.profile != null ? row.profile.MonthlyAmount : 0m, DueDay = row.profile != null ? row.profile.DueDayOfMonth : 1, IsActive = row.profile != null && row.profile.IsActive }).ToListAsync(cancellationToken);
    }
    public Task<Flat?> GetFlatAsync(Guid flatId, CancellationToken cancellationToken = default) => context.Flats.FindAsync([flatId], cancellationToken).AsTask();
    public Task<FlatBillingProfile?> GetProfileAsync(Guid flatId, CancellationToken cancellationToken = default) => context.FlatBillingProfiles.FirstOrDefaultAsync(profile => profile.FlatId == flatId, cancellationToken);
    public async Task SaveProfileAsync(FlatBillingProfile profile, CancellationToken cancellationToken = default) { if (context.Entry(profile).State == EntityState.Detached) context.FlatBillingProfiles.Add(profile); await context.SaveChangesAsync(cancellationToken); }
    public Task<TenantAssignment?> GetCurrentAssignmentAsync(Guid flatId, DateTime today, CancellationToken cancellationToken = default) => context.TenantAssignments.AsNoTracking().Where(assignment => assignment.FlatId == flatId && (assignment.EndDate == null || assignment.EndDate >= today)).OrderByDescending(assignment => assignment.StartDate).FirstOrDefaultAsync(cancellationToken);
    public Task<bool> TenantBillExistsAsync(Guid flatId, string tenantUserId, DateTime billDate, CancellationToken cancellationToken = default) => context.TenantBills.AnyAsync(bill => bill.FlatId == flatId && bill.TenantUserId == tenantUserId && bill.BillDate == billDate, cancellationToken);
    public async Task AddTenantBillAsync(TenantBill bill, CancellationToken cancellationToken = default) { await context.TenantBills.AddAsync(bill, cancellationToken); await context.SaveChangesAsync(cancellationToken); }
}
