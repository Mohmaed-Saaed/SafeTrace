using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafeTrace.Domain.Enums;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.ToTable("ApplicationUsers");

            builder.Property(u => u.FName)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(u => u.LName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.VerificationStatus)
                .IsRequired()
                .HasDefaultValue(VerificationStatus.Unverified);

            builder.HasMany(u => u.Cases)
                    .WithOne(c => c.User)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Notifications)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Complaints)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}