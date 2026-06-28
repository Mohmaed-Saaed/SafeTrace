using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class CaseConfiguration : IEntityTypeConfiguration<Case>
    {
        public void Configure(EntityTypeBuilder<Case> builder)
        {
            // Table
            builder.ToTable("Cases", t =>
            {
                t.HasCheckConstraint(
                    "CK_Cases_Age",
                    "[Age] >= 0 AND [Age] <= 120");
            });

            // Primary Key
            builder.HasKey(c => c.Id);

            // Enums
            builder.Property(c => c.Gender)
                .HasConversion<string>()
                .HasMaxLength(10);

            builder.Property(c => c.Status)
                .HasConversion<string>()
                .HasMaxLength(30);

            builder.Property(c => c.PreviousStatus)
                .HasConversion<string>()
                .HasMaxLength(30);

            builder.Property(c => c.CaseType)
                .HasConversion<string>()
                .HasMaxLength(30);

            builder.Property(c => c.Relation)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Properties
            builder.Property(c => c.CaseCode)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(c => c.EventDate)
                .IsRequired();

            builder.Property(c => c.UserId)
                .IsRequired();

            builder.Property(c => c.Government)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.City)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Street)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.FName)
                .IsRequired(false)
                .HasMaxLength(60);

            builder.Property(c => c.SName)
                .IsRequired(false)
                .HasMaxLength(60);

            builder.Property(c => c.TName)
                .IsRequired(false)
                .HasMaxLength(60);

            builder.Property(c => c.LName)
                .IsRequired(false)
                .HasMaxLength(60);

            builder.Property(c => c.CommunicationPhone)
                .HasMaxLength(15);

            builder.Property(c => c.Description)
                .HasMaxLength(2000);

            // Indexes
            builder.HasIndex(c => c.CaseCode)
                .IsUnique()
                .HasDatabaseName("UIX_Cases_CaseCode");

            builder.HasIndex(c => c.UserId);

            builder.HasIndex(c => c.Status);

            builder.HasIndex(c => c.CreatedAt);

            builder.HasIndex(c => c.AgeCategoryId);

            builder.HasIndex(c => new
            {
                c.Status,
                c.CaseType
            });

            // Relationships

            builder.HasOne(c => c.User)
                .WithMany(u => u.Cases)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.AgeCategory)
                .WithMany(a => a.Cases)
                .HasForeignKey(c => c.AgeCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.FoundPersonInfo)
                .WithOne(f => f.Case)
                .HasForeignKey<FoundPersonInfo>(f => f.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Photos)
                .WithOne(p => p.Case)
                .HasForeignKey(p => p.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Chats)
                .WithOne(ch => ch.Case)
                .HasForeignKey(ch => ch.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            // TPH Mapping
            builder.HasDiscriminator<string>("Discriminator")
                .HasValue<UrgentCase>("UrgentCase")
                .HasValue<LongTermMissingCase>("LongTermMissingCase")
                .HasValue<UnknownCase>("UnknownCase");
        }
    }
}