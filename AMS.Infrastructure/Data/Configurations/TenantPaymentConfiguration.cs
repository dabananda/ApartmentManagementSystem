using AMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Infrastructure.Data.Configurations;

public class TenantPaymentConfiguration : IEntityTypeConfiguration<TenantPayment>
{
    public void Configure(EntityTypeBuilder<TenantPayment> builder)
    {
        builder.HasIndex(x => new { x.TenantBillId, x.PaymentDate });
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.IdempotencyKey).HasMaxLength(80);
        builder.Property(x => x.ExternalRef).HasMaxLength(120);

        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL");

        builder.HasOne(x => x.TenantBill)
            .WithMany(b => b.Payments)
            .HasForeignKey(x => x.TenantBillId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasQueryFilter(x => !x.TenantBill!.IsDeleted);
    }
}
