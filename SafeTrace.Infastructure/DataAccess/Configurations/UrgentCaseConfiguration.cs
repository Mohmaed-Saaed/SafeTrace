using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafeTrace.Domain.Entities;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class UrgentCaseConfiguration : IEntityTypeConfiguration<UrgentCase>
    {
        public void Configure(EntityTypeBuilder<UrgentCase> builder)
        {
            builder.ToTable("UrgentCases");

            builder.Property(u => u.EndDate)
                .IsRequired();

            builder.Property(u => u.LimitReachDate)
                .IsRequired();
        }
    }
}