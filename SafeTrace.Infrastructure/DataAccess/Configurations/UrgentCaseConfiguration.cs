using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class UrgentCaseConfiguration : IEntityTypeConfiguration<UrgentCase>
    {
        public void Configure(EntityTypeBuilder<UrgentCase> builder)
        {
            // Properties
            builder.Property(x => x.EndDate)
                .IsRequired();

            builder.Property(x => x.LimitReachDate)
                .IsRequired();

            builder.Property(x => x.Location)
                .HasColumnType("geography");

            // Check Constraints (TPH-safe)
            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_UrgentCase_EndDate",
                    "([Discriminator] <> 'UrgentCase') OR ([EndDate] > [CreatedAt])");

                t.HasCheckConstraint(
                    "CK_UrgentCase_LimitReachDate",
                    "([Discriminator] <> 'UrgentCase') OR ([LimitReachDate] > [CreatedAt])");
            });

            // Indexes

            // Rate limit
            builder.HasIndex(x => new { x.UserId, x.LimitReachDate })
                .HasDatabaseName("IX_UrgentCases_UserId_LimitReachDate");

            // Expiration
            builder.HasIndex(x => new { x.Status, x.EndDate })
                .HasDatabaseName("IX_UrgentCases_Status_EndDate");

            // Public listing
            builder.HasIndex(x => new { x.Status, x.CreatedAt })
                .HasDatabaseName("IX_UrgentCases_Status_CreatedAt");

            // Spatial Index
            // Create in Database using raw SQL:
            //
            // CREATE SPATIAL INDEX IX_UrgentCases_Location
            // ON Cases(Location);
        }
    }
}