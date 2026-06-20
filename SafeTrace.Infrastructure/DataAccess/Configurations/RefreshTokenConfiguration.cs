using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(t => t.Id);
            builder.HasIndex(t => t.Token).IsUnique();
            builder.Property(t => t.Token).IsRequired().HasMaxLength(256);
            builder.Property(t => t.ReplacedByToken).HasMaxLength(256);

            builder.HasOne(t => t.User)
                   .WithMany(u => u.RefreshTokens)
                   .HasForeignKey(t => t.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}