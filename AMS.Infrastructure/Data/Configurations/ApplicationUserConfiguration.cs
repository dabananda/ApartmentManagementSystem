using AMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Infrastructure.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasMany(u => u.OwnedFlats)
            .WithOne(f => f.Owner)
            .HasForeignKey(f => f.OwnerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.Building)
            .WithMany()
            .HasForeignKey(u => u.BuildingId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(u => u.BuildingId);
    }
}
