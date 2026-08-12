using AMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Infrastructure.Data.Configurations;

public class MaintenanceTicketConfiguration : IEntityTypeConfiguration<MaintenanceTicket>
{
    public void Configure(EntityTypeBuilder<MaintenanceTicket> builder)
    {
        builder.HasIndex(x => new { x.BuildingId, x.Status, x.CreatedAt });
        builder.Property(x => x.Title).HasMaxLength(140).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();

        builder.HasIndex(t => new { t.BuildingId, t.CreatedByUserId });
        builder.HasIndex(t => new { t.BuildingId, t.FlatId, t.CreatedAt });
        
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
