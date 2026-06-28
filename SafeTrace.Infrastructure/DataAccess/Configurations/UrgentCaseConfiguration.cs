using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class UrgentCaseConfiguration : IEntityTypeConfiguration<UrgentCase>
    {
        public void Configure(EntityTypeBuilder<UrgentCase> builder)
        {
            builder.Property(u => u.EndDate)
                .IsRequired();

            builder.Property(u => u.LimitReachDate)
                .IsRequired();   
            
            builder.Property(x => x.Location)
                .HasColumnType("geography"); 

            builder.ToTable(t => t.HasCheckConstraint(
                "CK_UrgentCase_EndDate",
                "[EndDate] > [CreatedAt]"));

            builder.ToTable(t => t.HasCheckConstraint(
                "CK_UrgentCase_LimitReachDate",
                "[LimitReachDate] > [CreatedAt]"));

            // Rate-limit check: "does this user have an active window?"
            // Partial index only on non-deleted rows for efficiency.
            // NOTE: EF Core does not support partial indexes natively; apply via migration raw SQL.
            builder.HasIndex(x => new { x.UserId, x.LimitReachDate })
                   .HasDatabaseName("IX_UrgentCases_UserId_LimitReachDate");

            // Expiration job: "which active cases have passed EndDate?"
            builder.HasIndex(x => new { x.Status, x.EndDate })
                   .HasDatabaseName("IX_UrgentCases_Status_EndDate");

            // Public listing: ordered by creation date
            builder.HasIndex(x => new { x.Status, x.CreatedAt })
                   .HasDatabaseName("IX_UrgentCases_Status_CreatedAt");

            // Location spatial index — add via raw SQL in migration:
            //   CREATE SPATIAL INDEX IX_UrgentCases_Location ON UrgentCases(Location);
            // EF Core does not expose spatial index builder for all providers.
        }
    }
}
