using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for the application entities
        public DbSet<Building> Buildings { get; set; }
        public DbSet<Flat> Flats { get; set; }

        // Legacy tenant aggregate (kept for compatibility with older flows that reference it)
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Rent> Rents { get; set; }

        public DbSet<CommonBill> CommonBills { get; set; }
        public DbSet<ExpensePayment> ExpensePayments { get; set; }
        public DbSet<ExpenseAllocation> ExpenseAllocations { get; set; }
        public DbSet<ExpenseAllocationPayment> ExpenseAllocationPayments { get; set; }

        public DbSet<EntryLog> EntryLogs { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<MaintenanceTicket> MaintenanceTickets { get; set; }

        // Old owner-level profile (keeping if used elsewhere)
        public DbSet<OwnerBillingProfile> OwnerBillingProfiles => Set<OwnerBillingProfile>();

        // New owner→tenant billing flow
        public DbSet<TenantAssignment> TenantAssignments { get; set; }
        public DbSet<FlatBillingProfile> FlatBillingProfiles { get; set; }
        public DbSet<TenantBill> TenantBills { get; set; }
        public DbSet<TenantPayment> TenantPayments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Building → Flats
            modelBuilder.Entity<Building>()
                .HasMany(b => b.Flats)
                .WithOne(f => f.Building)
                .HasForeignKey(f => f.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);

            // User → OwnedFlats
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.OwnedFlats)
                .WithOne(f => f.Owner)
                .HasForeignKey(f => f.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            // User → Building
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Building)
                .WithMany()
                .HasForeignKey(u => u.BuildingId)
                .OnDelete(DeleteBehavior.SetNull);

            // Flat ↔ Legacy Tenant aggregate
            modelBuilder.Entity<Tenant>()
                .HasOne(t => t.Flat)
                .WithMany(f => f.Tenants)
                .HasForeignKey(t => t.FlatId)
                .OnDelete(DeleteBehavior.Restrict);

            // Legacy Tenant ↔ Identity user link
            modelBuilder.Entity<Tenant>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ensure 1:1 mapping of (legacy) Tenant → User (optional/nullable)
            modelBuilder.Entity<Tenant>()
                .HasIndex(t => t.UserId)
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL");

            // CommonBill → ExpensePayment (prevent cascade loop)
            modelBuilder.Entity<ExpensePayment>()
                .HasOne(ep => ep.CommonBill)
                .WithMany()
                .HasForeignKey(ep => ep.CommonBillId)
                .OnDelete(DeleteBehavior.NoAction);

            // Owner payments under allocation
            modelBuilder.Entity<ExpenseAllocationPayment>(e =>
            {
                e.HasIndex(x => new { x.CommonBillId, x.OwnerId, x.PaymentDate });
                e.Property(x => x.Amount).HasColumnType("decimal(18, 2)");

                e.HasOne(x => x.ExpenseAllocation)
                 .WithMany(a => a.Payments)
                 .HasForeignKey(x => x.ExpenseAllocationId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // EntryLog
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

            // Announcements
            modelBuilder.Entity<Announcement>(e =>
            {
                e.HasIndex(x => new { x.BuildingId, x.CreatedAt });
                e.Property(x => x.Title).HasMaxLength(120).IsRequired();
                e.Property(x => x.Body).HasMaxLength(2000).IsRequired();
            });

            // Maintenance Tickets
            modelBuilder.Entity<MaintenanceTicket>(e =>
            {
                e.HasIndex(x => new { x.BuildingId, x.Status, x.CreatedAt });
                e.Property(x => x.Title).HasMaxLength(140).IsRequired();
                e.Property(x => x.Description).HasMaxLength(2000).IsRequired();
                e.Property(x => x.Status).HasMaxLength(20).IsRequired();

                e.HasIndex(t => new { t.BuildingId, t.CreatedByUserId });
                e.HasIndex(t => new { t.BuildingId, t.FlatId, t.CreatedAt });
            });

            // Unique building fields
            modelBuilder.Entity<Building>()
                .HasIndex(b => b.Name)
                .IsUnique();

            modelBuilder.Entity<Building>()
                .HasIndex(b => b.Code)
                .IsUnique();

            // Helpful indexes
            modelBuilder.Entity<Flat>()
                .HasIndex(f => new { f.BuildingId, f.FlatNumber })
                .IsUnique();

            modelBuilder.Entity<Flat>()
                .HasIndex(f => f.OwnerId);

            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.BuildingId);

            // Owner-level (legacy) profile (if still used)
            modelBuilder.Entity<OwnerBillingProfile>(e =>
            {
                e.HasIndex(x => x.FlatId).IsUnique();
            });

            // —— New owner→tenant rent flow ——

            // TenantAssignment (active one per flat at a time; enforced by app logic)
            modelBuilder.Entity<TenantAssignment>(e =>
            {
                e.HasIndex(x => new { x.FlatId, x.TenantUserId, x.StartDate });
                e.HasOne(x => x.Flat).WithMany().HasForeignKey(x => x.FlatId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.TenantUser).WithMany().HasForeignKey(x => x.TenantUserId).OnDelete(DeleteBehavior.Restrict);
            });

            // FlatBillingProfile (1 per flat)
            modelBuilder.Entity<FlatBillingProfile>(e =>
            {
                e.HasIndex(x => x.FlatId).IsUnique();
                e.Property(x => x.MonthlyAmount).HasColumnType("decimal(18,2)");
                e.HasOne(x => x.Flat).WithMany().HasForeignKey(x => x.FlatId).OnDelete(DeleteBehavior.Cascade);
            });

            // TenantBill (Identity-based tenant billing)
            modelBuilder.Entity<TenantBill>(e =>
            {
                e.HasIndex(x => new { x.TenantUserId, x.BillDate });
                e.Property(x => x.Amount).HasColumnType("decimal(18,2)");

                e.HasOne(x => x.Flat)
                 .WithMany()
                 .HasForeignKey(x => x.FlatId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.TenantUser)
                 .WithMany()
                 .HasForeignKey(x => x.TenantUserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // TenantPayment
            modelBuilder.Entity<TenantPayment>(e =>
            {
                e.HasIndex(x => new { x.TenantBillId, x.PaymentDate });
                e.Property(x => x.Amount).HasColumnType("decimal(18,2)");

                e.HasOne(x => x.TenantBill)
                 .WithMany(b => b.Payments)
                 .HasForeignKey(x => x.TenantBillId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // Legacy Rent links to (new) TenantBill if present
            modelBuilder.Entity<Rent>()
                .HasOne(r => r.TenantBill)
                .WithMany()
                .HasForeignKey(r => r.TenantBillId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
