using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class CaseConfiguration : IEntityTypeConfiguration<Case>
    {
        public void Configure(EntityTypeBuilder<Case> builder)
        {
            builder.ToTable("Cases");

            builder.Property(x => x.Gender)
                   .HasConversion<int>();

            builder.Property(x => x.Status)
                   .HasConversion<int>();

            builder.Property(x => x.CaseType)
                   .HasConversion<int>();

            builder.Property(x => x.Relation)
                   .HasConversion<int>();

            builder.Property(x => x.PreviousStatus)
                   .HasConversion<int>();

            builder.HasKey(c => c.Id);

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
                .HasDefaultValue(10000);

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
            .HasValue<UrgentCase>("UrgentCase")
            .HasValue<LongTermMissingCase>("LongTermMissingCase")
            .HasValue<UnknownCase>("UnknownCase");
        }
    }
}