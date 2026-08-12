using AMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Infrastructure.Data.Configurations;

public class RentConfiguration : IEntityTypeConfiguration<Rent>
{
    public void Configure(EntityTypeBuilder<Rent> builder)
    {
        builder.Property(r => r.Amount).HasColumnType("decimal(18,2)");
        builder.HasOne(r => r.TenantBill)
            .WithMany()
            .HasForeignKey(r => r.TenantBillId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasQueryFilter(x => !x.Tenant!.Flat!.IsDeleted);
    }
}
