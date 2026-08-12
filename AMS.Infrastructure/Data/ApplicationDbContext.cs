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

        modelBuilder.Entity<Building>()
            .HasMany(b => b.Flats)
            .WithOne(f => f.Building)
            .HasForeignKey(f => f.BuildingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Building>()
            .HasIndex(b => b.Name)
            .IsUnique();

        modelBuilder.Entity<Building>()
            .HasIndex(b => b.Code)
            .IsUnique();

        modelBuilder.Entity<ApplicationUser>()
            .HasMany(u => u.OwnedFlats)
            .WithOne(f => f.Owner)
            .HasForeignKey(f => f.OwnerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.Building)
            .WithMany()
            .HasForeignKey(u => u.BuildingId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ApplicationUser>()
            .HasIndex(u => u.BuildingId);

        modelBuilder.Entity<Flat>()
            .HasIndex(f => new { f.BuildingId, f.FlatNumber })
            .IsUnique();
        modelBuilder.Entity<Flat>()
            .HasIndex(f => f.OwnerId);

        modelBuilder.Entity<Tenant>()
            .HasOne(t => t.Flat)
            .WithMany(f => f.Tenants)
            .HasForeignKey(t => t.FlatId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Tenant>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.UserId)
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL");

        modelBuilder.Entity<EntryLog>()
            .HasOne(el => el.Building)
            .WithMany(b => b.EntryLogs)
            .HasForeignKey(el => el.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EntryLog>()
            .HasOne(el => el.Flat)
            .WithMany()
            .HasForeignKey(el => el.FlatId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Announcement>(e =>
        {
            e.HasIndex(x => new { x.BuildingId, x.CreatedAt });
            e.Property(x => x.Title).HasMaxLength(120).IsRequired();
            e.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        });

        modelBuilder.Entity<MaintenanceTicket>(e =>
        {
            e.HasIndex(x => new { x.BuildingId, x.Status, x.CreatedAt });
            e.Property(x => x.Title).HasMaxLength(140).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();

            e.HasIndex(t => new { t.BuildingId, t.CreatedByUserId });
            e.HasIndex(t => new { t.BuildingId, t.FlatId, t.CreatedAt });
        });

        modelBuilder.Entity<ExpensePayment>()
            .HasOne(ep => ep.CommonBill)
            .WithMany()
            .HasForeignKey(ep => ep.CommonBillId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ExpenseAllocation>(e =>
        {
            e.Property(x => x.AmountDue).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.CommonBill)
                .WithMany(b => b.Allocations)
                .HasForeignKey(x => x.CommonBillId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Owner)
                .WithMany()
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExpenseAllocationPayment>(e =>
        {
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.Property(x => x.IdempotencyKey).HasMaxLength(80);
            e.Property(x => x.ExternalRef).HasMaxLength(120);
            e.HasIndex(x => x.IdempotencyKey)
                .IsUnique()
                .HasFilter("[IdempotencyKey] IS NOT NULL");
            e.HasIndex(x => new { x.CommonBillId, x.OwnerId, x.PaymentDate });
            e.HasOne(x => x.ExpenseAllocation)
                .WithMany(a => a.Payments)
                .HasForeignKey(x => x.ExpenseAllocationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TenantAssignment>(e =>
        {
            e.HasIndex(x => new { x.FlatId, x.TenantUserId, x.StartDate });

            e.HasIndex(x => x.TenantUserId)
                .HasFilter("[EndDate] IS NULL")
                .IsUnique()
                .HasDatabaseName("IX_TenantAssignments_TenantUserId_Active");

            e.HasIndex(x => x.FlatId)
                .HasFilter("[EndDate] IS NULL")
                .IsUnique()
                .HasDatabaseName("IX_TenantAssignments_FlatId_Active");

            e.HasOne(x => x.Flat)
                .WithMany()
                .HasForeignKey(x => x.FlatId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.TenantUser)
                .WithMany()
                .HasForeignKey(x => x.TenantUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FlatBillingProfile>(e =>
        {
            e.HasIndex(x => x.FlatId).IsUnique();
            e.Property(x => x.MonthlyAmount).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Flat)
                .WithMany()
                .HasForeignKey(x => x.FlatId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TenantBill>(e =>
        {
            e.HasIndex(x => new { x.TenantUserId, x.BillDate });
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.Property(x => x.RowVersion).IsRowVersion();

            e.HasOne(x => x.Flat)
                .WithMany()
                .HasForeignKey(x => x.FlatId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.TenantUser)
                .WithMany()
                .HasForeignKey(x => x.TenantUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TenantPayment>(e =>
        {
            e.HasIndex(x => new { x.TenantBillId, x.PaymentDate });
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.Property(x => x.IdempotencyKey).HasMaxLength(80);
            e.Property(x => x.ExternalRef).HasMaxLength(120);

            e.HasIndex(x => x.IdempotencyKey)
                .IsUnique()
                .HasFilter("[IdempotencyKey] IS NOT NULL");

            e.HasOne(x => x.TenantBill)
                .WithMany(b => b.Payments)
                .HasForeignKey(x => x.TenantBillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Rent>()
            .Property(r => r.Amount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Rent>()
            .HasOne(r => r.TenantBill)
            .WithMany()
            .HasForeignKey(r => r.TenantBillId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Building>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Flat>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<CommonBill>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<ExpensePayment>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Announcement>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<TenantBill>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<MaintenanceTicket>().HasQueryFilter(x => !x.IsDeleted);

        // Matching query filters for dependent entities to resolve EF Core warnings
        modelBuilder.Entity<EntryLog>().HasQueryFilter(x => !x.Building!.IsDeleted && !x.Flat!.IsDeleted);
        modelBuilder.Entity<ExpenseAllocation>().HasQueryFilter(x => !x.CommonBill!.IsDeleted);
        modelBuilder.Entity<FlatBillingProfile>().HasQueryFilter(x => !x.Flat!.IsDeleted);
        modelBuilder.Entity<OwnerBillingProfile>().HasQueryFilter(x => !x.Flat!.IsDeleted);
        modelBuilder.Entity<Tenant>().HasQueryFilter(x => !x.Flat!.IsDeleted);
        modelBuilder.Entity<TenantAssignment>().HasQueryFilter(x => !x.Flat!.IsDeleted);
        modelBuilder.Entity<TenantPayment>().HasQueryFilter(x => !x.TenantBill!.IsDeleted);
        modelBuilder.Entity<ExpenseAllocationPayment>().HasQueryFilter(x => !x.ExpenseAllocation!.CommonBill!.IsDeleted);
        modelBuilder.Entity<Rent>().HasQueryFilter(x => !x.Tenant!.Flat!.IsDeleted);
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
