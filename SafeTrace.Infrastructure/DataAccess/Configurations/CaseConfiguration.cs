using Microsoft.EntityFrameworkCore;
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
                    "[Age] BETWEEN 0 AND 120");
            });

            // Primary Key
            builder.HasKey(c => c.Id);

            // Enums
            builder.Property(x => x.Gender)
                .HasConversion<string>();

            builder.Property(x => x.Status)
                .HasConversion<string>();

            builder.Property(x => x.CaseType)
                .HasConversion<string>();

            builder.Property(x => x.Relation)
                .HasConversion<string>();

            builder.Property(x => x.PreviousStatus)
                .HasConversion<string>();

            // Properties
            builder.Property(c => c.CaseCode)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

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
                .IsRequired()
                .HasMaxLength(60);

            builder.Property(c => c.SName)
                .IsRequired()
                .HasMaxLength(60);

            builder.Property(c => c.TName)
                .IsRequired()
                .HasMaxLength(60);

            builder.Property(c => c.LName)
                .IsRequired()
                .HasMaxLength(60);

            builder.Property(c => c.CommunicationPhone)
                .HasMaxLength(20);

            builder.Property(c => c.Description)
                .HasMaxLength(2000);

            // Indexes
            builder.HasIndex(c => c.CaseCode)
                .IsUnique()
                .HasDatabaseName("UIX_Cases_CaseCode");

            // Relationships
            builder.HasOne(c => c.User)
                .WithMany(u => u.Cases)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.AgeCategory)
                .WithMany(a => a.Cases)
                .HasForeignKey(c => c.AgeCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Photos)
                .WithOne(p => p.Case)
                .HasForeignKey(p => p.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Chats)
                .WithOne(ch => ch.Case)
                .HasForeignKey(ch => ch.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Table Per Hierarchy (TPH)
            builder.HasDiscriminator<string>("Discriminator")
                .HasValue<Case>("Case")
                .HasValue<UrgentCase>("UrgentCase")
                .HasValue<LongTermMissingCase>("LongTermMissingCase")
                .HasValue<UnknownCase>("UnknownCase");
        }
    }
}