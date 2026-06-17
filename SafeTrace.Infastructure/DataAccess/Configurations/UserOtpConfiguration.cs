using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    public class UserOtpConfiguration : IEntityTypeConfiguration<UserOtp>
    {
        public void Configure(EntityTypeBuilder<UserOtp> builder)
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Code).IsRequired().HasMaxLength(6);
            builder.Property(o => o.Type).HasConversion<string>().HasMaxLength(30);

            builder.HasOne(o => o.User)
                   .WithMany(u => u.UserOtps)
                   .HasForeignKey(o => o.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}