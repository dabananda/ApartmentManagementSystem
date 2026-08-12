using AMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Infrastructure.Data.Configurations;

public class FlatConfiguration : IEntityTypeConfiguration<Flat>
{
    public void Configure(EntityTypeBuilder<Flat> builder)
    {
        builder.HasIndex(f => new { f.BuildingId, f.FlatNumber }).IsUnique();
        builder.HasIndex(f => f.OwnerId);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
