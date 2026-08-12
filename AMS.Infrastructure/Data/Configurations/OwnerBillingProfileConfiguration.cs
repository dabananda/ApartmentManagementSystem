using AMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Infrastructure.Data.Configurations;

public class OwnerBillingProfileConfiguration : IEntityTypeConfiguration<OwnerBillingProfile>
{
    public void Configure(EntityTypeBuilder<OwnerBillingProfile> builder)
    {
        builder.HasQueryFilter(x => !x.Flat!.IsDeleted);
    }
}
