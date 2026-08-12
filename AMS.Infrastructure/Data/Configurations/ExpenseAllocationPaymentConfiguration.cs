using AMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Infrastructure.Data.Configurations;

public class ExpenseAllocationPaymentConfiguration : IEntityTypeConfiguration<ExpenseAllocationPayment>
{
    public void Configure(EntityTypeBuilder<ExpenseAllocationPayment> builder)
    {
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.IdempotencyKey).HasMaxLength(80);
        builder.Property(x => x.ExternalRef).HasMaxLength(120);
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL");
        builder.HasIndex(x => new { x.CommonBillId, x.OwnerId, x.PaymentDate });
        builder.HasOne(x => x.ExpenseAllocation)
            .WithMany(a => a.Payments)
            .HasForeignKey(x => x.ExpenseAllocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.ExpenseAllocation!.CommonBill!.IsDeleted);
    }
}
