using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class LongTermMissingCaseConfiguration : IEntityTypeConfiguration<LongTermMissingCase>
    {
        public void Configure(EntityTypeBuilder<LongTermMissingCase> builder)
        {
            builder.ToTable("LongTermMissingCases");

            builder.Property(l => l.Government)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(l => l.City)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(l => l.Street)
                .HasMaxLength(500);
        }
    }
}