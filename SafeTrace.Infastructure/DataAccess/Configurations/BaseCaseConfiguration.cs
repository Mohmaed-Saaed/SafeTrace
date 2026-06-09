using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafeTrace.Domain.Entities;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class BaseCaseConfiguration : IEntityTypeConfiguration<BaseCase>
    {
        public void Configure(EntityTypeBuilder<BaseCase> builder)
        {
            builder.ToTable("BaseCases");

            builder.HasKey(c => c.Id);

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
                .WithMany(ac => ac.BaseCases)
                .HasForeignKey(c => c.AgeCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}