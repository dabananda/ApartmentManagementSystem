using AMS.Domain.Entities;
using AMS.Domain.Entities.Base;
using AMS.Infrastructure.Services;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AMS.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Building> Buildings { get; set; }
    public DbSet<Flat> Flats { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Rent> Rents { get; set; }

    public DbSet<CommonBill> CommonBills { get; set; }
    public DbSet<ExpensePayment> ExpensePayments { get; set; }
    public DbSet<ExpenseAllocation> ExpenseAllocations { get; set; }
    public DbSet<ExpenseAllocationPayment> ExpenseAllocationPayments { get; set; }

    public DbSet<EntryLog> EntryLogs { get; set; }
    public DbSet<Announcement> Announcements { get; set; }
    public DbSet<MaintenanceTicket> MaintenanceTickets { get; set; }
    public DbSet<OwnerBillingProfile> OwnerBillingProfiles => Set<OwnerBillingProfile>();
    public DbSet<TenantAssignment> TenantAssignments { get; set; }
    public DbSet<FlatBillingProfile> FlatBillingProfiles { get; set; }
    public DbSet<TenantBill> TenantBills { get; set; }
    public DbSet<TenantPayment> TenantPayments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ams");

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        ApplyAuditAndSoftDeleteRules();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDeleteRules();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditAndSoftDeleteRules()
    {
        var entries = ChangeTracker.Entries();
        var userId = _currentUserService?.UserId;

        foreach (var entry in entries)
        {
            if (entry.Entity is IAuditableEntity auditableEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    auditableEntity.CreatedAt = DateTime.UtcNow;
                    auditableEntity.CreatedBy = userId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditableEntity.UpdatedAt = DateTime.UtcNow;
                    auditableEntity.UpdatedBy = userId;
                }
            }

            if (entry.Entity is ISoftDeletable softDeletableEntity)
            {
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    softDeletableEntity.IsDeleted = true;
                    softDeletableEntity.DeletedAt = DateTime.UtcNow;

                    if (entry.Entity is IAuditableEntity auditable)
                    {
                        auditable.UpdatedAt = DateTime.UtcNow;
                        auditable.UpdatedBy = userId;
                    }
                }
            }
        }
    }
}
