using AMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Infrastructure.Data.Configurations;

public class FlatBillingProfileConfiguration : IEntityTypeConfiguration<FlatBillingProfile>
{
    public void Configure(EntityTypeBuilder<FlatBillingProfile> builder)
    {
        builder.HasIndex(x => x.FlatId).IsUnique();
        builder.Property(x => x.MonthlyAmount).HasColumnType("decimal(18,2)");
        builder.HasOne(x => x.Flat)
            .WithMany()
            .HasForeignKey(x => x.FlatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.Flat!.IsDeleted);
    }
}
