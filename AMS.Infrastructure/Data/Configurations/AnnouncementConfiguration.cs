using AMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Infrastructure.Data.Configurations;

public class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.HasIndex(x => new { x.BuildingId, x.CreatedAt });
        builder.Property(x => x.Title).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
