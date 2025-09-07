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
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Rent> Rents { get; set; }
        public DbSet<CommonBill> CommonBills { get; set; }
        public DbSet<ExpensePayment> ExpensePayments { get; set; }
        public DbSet<ExpenseAllocation> ExpenseAllocations { get; set; }
        public DbSet<EntryLog> EntryLogs { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<MaintenanceTicket> MaintenanceTickets { get; set; }
        public DbSet<TenantBill> TenantBills => Set<TenantBill>();
        public DbSet<OwnerBillingProfile> OwnerBillingProfiles => Set<OwnerBillingProfile>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Building → Flats relationship (One-to-Many)
            modelBuilder.Entity<Building>()
                .HasMany(b => b.Flats)
                .WithOne(f => f.Building)
                .HasForeignKey(f => f.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ApplicationUser → OwnedFlats relationship (One-to-Many)
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.OwnedFlats)
                .WithOne(f => f.Owner)
                .HasForeignKey(f => f.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure ApplicationUser → Building relationship (Many-to-One)
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Building)
                .WithMany()
                .HasForeignKey(u => u.BuildingId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Flat → Tenants relationship to prevent cascade issues
            modelBuilder.Entity<Tenant>()
                .HasOne(t => t.Flat)
                .WithMany(f => f.Tenants)
                .HasForeignKey(t => t.FlatId)
                .OnDelete(DeleteBehavior.Restrict); // Changed from default CASCADE to RESTRICT

            // Configure TenantBill relationships to prevent cascade cycles
            modelBuilder.Entity<TenantBill>()
                .HasOne(tb => tb.Flat)
                .WithMany()
                .HasForeignKey(tb => tb.FlatId)
                .OnDelete(DeleteBehavior.Restrict); // Changed from CASCADE to RESTRICT

            modelBuilder.Entity<TenantBill>()
                .HasOne(tb => tb.Tenant)
                .WithMany()
                .HasForeignKey(tb => tb.TenantId)
                .OnDelete(DeleteBehavior.Restrict); // Changed from CASCADE to RESTRICT

            // Configure CommonBill → ExpensePayment relationship to prevent cascade loop
            modelBuilder.Entity<ExpensePayment>()
                .HasOne(ep => ep.CommonBill)
                .WithMany()
                .HasForeignKey(ep => ep.CommonBillId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure EntryLog relationships
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

            // Configure Tenant → User relationship
            modelBuilder.Entity<Tenant>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Optional: unique constraint so a user maps to at most one Tenant
            modelBuilder.Entity<Tenant>()
                .HasIndex(t => t.UserId)
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL");

            // Announcement
            modelBuilder.Entity<Announcement>(e =>
            {
                e.HasIndex(x => new { x.BuildingId, x.CreatedAt });
                e.Property(x => x.Title).HasMaxLength(120).IsRequired();
                e.Property(x => x.Body).HasMaxLength(2000).IsRequired();
            });

            // MaintenanceTicket
            modelBuilder.Entity<MaintenanceTicket>(e =>
            {
                e.HasIndex(x => new { x.BuildingId, x.Status, x.CreatedAt });
                e.Property(x => x.Title).HasMaxLength(140).IsRequired();
                e.Property(x => x.Description).HasMaxLength(2000).IsRequired();
                e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            });

            // Add unique constraints and indexes for performance
            modelBuilder.Entity<Building>()
                .HasIndex(b => b.Name)
                .IsUnique();

            modelBuilder.Entity<Flat>()
                .HasIndex(f => new { f.BuildingId, f.FlatNumber })
                .IsUnique();

            modelBuilder.Entity<OwnerBillingProfile>(e =>
            {
                e.HasIndex(x => x.FlatId).IsUnique(); // 1 profile per flat
            });

            modelBuilder.Entity<TenantBill>(e =>
            {
                e.HasIndex(x => new { x.FlatId, x.Year, x.Month }).IsUnique();
                e.Property(x => x.Status).HasMaxLength(16).IsRequired();
                e.Property(x => x.RowVersion).IsRowVersion();
            });

            modelBuilder.Entity<Rent>()
                .HasOne(r => r.TenantBill)
                .WithMany()
                .HasForeignKey(r => r.TenantBillId)
                .OnDelete(DeleteBehavior.SetNull);

            // Add index on commonly queried fields
            modelBuilder.Entity<Flat>()
                .HasIndex(f => f.OwnerId);

            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.BuildingId);

            modelBuilder.Entity<MaintenanceTicket>()
                .HasIndex(t => new { t.BuildingId, t.CreatedByUserId });

            modelBuilder.Entity<MaintenanceTicket>()
                .HasIndex(t => new { t.BuildingId, t.FlatId, t.CreatedAt });
        }
    }
}
