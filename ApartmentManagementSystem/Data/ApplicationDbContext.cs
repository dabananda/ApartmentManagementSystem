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
        public DbSet<CommonExpense> CommonExpenses { get; set; }
        public DbSet<ExpenseAllocation> ExpenseAllocations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Building → Flats relationship (One-to-Many)
            modelBuilder.Entity<Building>()
                .HasMany(b => b.Flats)               // Building has many Flats
                .WithOne(f => f.Building)           // Each Flat has one Building
                .HasForeignKey(f => f.BuildingId)   // Foreign key is BuildingId
                .OnDelete(DeleteBehavior.Cascade);  // Delete flats when building deleted

            // Configure ApplicationUser → OwnedFlats relationship (One-to-Many)
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.OwnedFlats)         // User has many owned flats
                .WithOne(f => f.Owner)              // Each Flat has one owner
                .HasForeignKey(f => f.OwnerId)      // Foreign key is OwnerId
                .OnDelete(DeleteBehavior.SetNull);  // Set OwnerId to null when user deleted

            // Configure ApplicationUser → Building relationship (Many-to-One)
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Building)            // User has one building
                .WithMany()                         // Building can have many users (no navigation property needed)
                .HasForeignKey(u => u.BuildingId)   // Foreign key is BuildingId
                .OnDelete(DeleteBehavior.SetNull);  // Set BuildingId to null when building deleted

            // Add unique constraints and indexes for performance
            modelBuilder.Entity<Building>()
                .HasIndex(b => b.Name)
                .IsUnique();                        // Building names must be unique

            modelBuilder.Entity<Flat>()
                .HasIndex(f => new { f.BuildingId, f.FlatNumber })
                .IsUnique();                        // Flat numbers must be unique within each building

            // Add index on commonly queried fields
            modelBuilder.Entity<Flat>()
                .HasIndex(f => f.OwnerId);

            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.BuildingId);
        }
    }
}
