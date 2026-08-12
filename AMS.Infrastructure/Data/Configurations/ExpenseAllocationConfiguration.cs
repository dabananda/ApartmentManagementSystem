using AMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Infrastructure.Data.Configurations;

public class ExpenseAllocationConfiguration : IEntityTypeConfiguration<ExpenseAllocation>
{
    public void Configure(EntityTypeBuilder<ExpenseAllocation> builder)
    {
        builder.Property(x => x.AmountDue).HasColumnType("decimal(18,2)");
        builder.HasOne(x => x.CommonBill)
            .WithMany(b => b.Allocations)
            .HasForeignKey(x => x.CommonBillId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Owner)
            .WithMany()
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasQueryFilter(x => !x.CommonBill!.IsDeleted);
    }
}
