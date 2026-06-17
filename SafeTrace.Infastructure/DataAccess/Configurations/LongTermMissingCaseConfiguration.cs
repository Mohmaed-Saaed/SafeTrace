using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class LongTermMissingCaseConfiguration : IEntityTypeConfiguration<LongTermMissingCase>
    {
        public void Configure(EntityTypeBuilder<LongTermMissingCase> builder)
        {
            builder.ToTable("LongTermMissingCases");
        }
    }
}