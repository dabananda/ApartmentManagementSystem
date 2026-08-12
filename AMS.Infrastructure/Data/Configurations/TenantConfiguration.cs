using AMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Infrastructure.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasOne(t => t.Flat)
            .WithMany(f => f.Tenants)
            .HasForeignKey(t => t.FlatId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.UserId)
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL");
            
        builder.HasQueryFilter(x => !x.Flat!.IsDeleted);
    }
}
