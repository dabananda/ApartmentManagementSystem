using AMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Infrastructure.Data.Configurations;

public class TenantAssignmentConfiguration : IEntityTypeConfiguration<TenantAssignment>
{
    public void Configure(EntityTypeBuilder<TenantAssignment> builder)
    {
        builder.HasIndex(x => new { x.FlatId, x.TenantUserId, x.StartDate });

        builder.HasIndex(x => x.TenantUserId)
            .HasFilter("[EndDate] IS NULL")
            .IsUnique()
            .HasDatabaseName("IX_TenantAssignments_TenantUserId_Active");

        builder.HasIndex(x => x.FlatId)
            .HasFilter("[EndDate] IS NULL")
            .IsUnique()
            .HasDatabaseName("IX_TenantAssignments_FlatId_Active");

        builder.HasOne(x => x.Flat)
            .WithMany()
            .HasForeignKey(x => x.FlatId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TenantUser)
            .WithMany()
            .HasForeignKey(x => x.TenantUserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasQueryFilter(x => !x.Flat!.IsDeleted);
    }
}
