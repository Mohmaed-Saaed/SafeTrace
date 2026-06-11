using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class FoundPersonInfoConfiguration : IEntityTypeConfiguration<FoundPersonInfo>
    {
        public void Configure(EntityTypeBuilder<FoundPersonInfo> builder)
        {
            builder.ToTable("FoundPersonInfos");

            builder.HasKey(f => f.Id);

            builder.Property(f => f.Government)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(f => f.City)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(f => f.Street)
                .HasMaxLength(500);

            builder.HasOne(f => f.Case)
                .WithOne(c => c.FoundPersonInfo)
                .HasForeignKey<FoundPersonInfo>(f => f.CaseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.FoundedUser)
                .WithMany()
                .HasForeignKey(f => f.FoundedUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}