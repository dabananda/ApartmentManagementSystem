using AMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Infrastructure.Data.Configurations;

public class EntryLogConfiguration : IEntityTypeConfiguration<EntryLog>
{
    public void Configure(EntityTypeBuilder<EntryLog> builder)
    {
        builder.HasOne(el => el.Building)
            .WithMany(b => b.EntryLogs)
            .HasForeignKey(el => el.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(el => el.Flat)
            .WithMany()
            .HasForeignKey(el => el.FlatId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.Building!.IsDeleted && !x.Flat!.IsDeleted);
    }
}
