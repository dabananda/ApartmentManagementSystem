using AMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Infrastructure.Data.Configurations;

public class CommonBillConfiguration : IEntityTypeConfiguration<CommonBill>
{
    public void Configure(EntityTypeBuilder<CommonBill> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
