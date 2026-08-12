using AMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Infrastructure.Data.Configurations;

public class TenantBillConfiguration : IEntityTypeConfiguration<TenantBill>
{
    public void Configure(EntityTypeBuilder<TenantBill> builder)
    {
        builder.HasIndex(x => new { x.TenantUserId, x.BillDate });
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne(x => x.Flat)
            .WithMany()
            .HasForeignKey(x => x.FlatId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TenantUser)
            .WithMany()
            .HasForeignKey(x => x.TenantUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
