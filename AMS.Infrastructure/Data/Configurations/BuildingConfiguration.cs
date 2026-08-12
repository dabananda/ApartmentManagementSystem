using AMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Infrastructure.Data.Configurations;

public class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        builder.HasMany(b => b.Flats)
            .WithOne(f => f.Building)
            .HasForeignKey(f => f.BuildingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => b.Name).IsUnique();
        builder.HasIndex(b => b.Code).IsUnique();
        
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
