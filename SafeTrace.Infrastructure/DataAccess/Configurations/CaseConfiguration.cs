using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class CaseConfiguration : IEntityTypeConfiguration<Case>
    {
        public void Configure(EntityTypeBuilder<Case> builder)
        {
            builder.ToTable("Cases");

            builder.HasKey(c => c.Id);

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

            builder.Property(c => c.Government)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.City)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Street)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(c => c.CaseCode)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.FName)
                .HasMaxLength(60);

            builder.Property(x => x.SName)
                .HasMaxLength(60);

            builder.Property(x => x.TName)
                .HasMaxLength(60);

            builder.Property(x => x.LName)
                .HasMaxLength(60);

            builder.Property(x => x.CommunicationPhone)
                .HasMaxLength(20);

            builder.Property(x => x.Description)
                .HasMaxLength(2000);

            builder.ToTable(t => t.HasCheckConstraint(
                "CK_UrgentCase_Age",
                "[Age] BETWEEN 0 AND 120"));

            builder.HasOne(c => c.User)
                .WithMany(u => u.Cases)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Photos)
                .WithOne(p => p.Case)
                .HasForeignKey(p => p.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Chats)
                .WithOne(ch => ch.Case)
                .HasForeignKey(ch => ch.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.AgeCategory)
                .WithMany(ac => ac.Cases)
                .HasForeignKey(c => c.AgeCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasDiscriminator<string>("Discriminator")
                .HasValue<Case>("Case")
                .HasValue<UrgentCase>("UrgentCase")
                .HasValue<LongTermMissingCase>("LongTermMissingCase")
                .HasValue<UnknownCase>("UnknownCase");

            builder.HasIndex(x => x.CaseCode)
                .IsUnique()
                .HasDatabaseName("UIX_UrgentCases_CaseCode");
        }
    }
}