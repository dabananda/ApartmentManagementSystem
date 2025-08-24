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

            // Configure CommonBill → ExpensePayment relationship to prevent cascade loop
            modelBuilder.Entity<ExpensePayment>()
                .HasOne(ep => ep.CommonBill)
                .WithMany()
                .HasForeignKey(ep => ep.CommonBillId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure CommonBill → ExpenseAllocation relationship to prevent cascade loop
            //modelBuilder.Entity<ExpenseAllocation>()
            //    .HasOne(ea => ea.CommonBill)
            //    .WithMany()
            //    .HasForeignKey(ea => ea.CommonBillId)
            //    .OnDelete(DeleteBehavior.NoAction);

            // Add unique constraints and indexes for performance
            modelBuilder.Entity<Building>()
                .HasIndex(b => b.Name)
                .IsUnique();

            modelBuilder.Entity<Flat>()
                .HasIndex(f => new { f.BuildingId, f.FlatNumber })
                .IsUnique();

            // Add index on commonly queried fields
            modelBuilder.Entity<Flat>()
                .HasIndex(f => f.OwnerId);

            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.BuildingId);
        }
    }
}