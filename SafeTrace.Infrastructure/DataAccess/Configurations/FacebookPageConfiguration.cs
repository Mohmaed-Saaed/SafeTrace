using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class FacebookPageConfiguration : IEntityTypeConfiguration<FacebookPage>
    {
        public void Configure(EntityTypeBuilder<FacebookPage> builder)
        {
            builder.ToTable("FacebookPages");

            builder.HasKey(page => page.Id);

            builder.Property(page => page.FacebookPageId)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(page => page.FacebookPageId)
                .IsUnique();

            builder.Property(page => page.PageName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(page => page.PageUrl)
                .IsRequired(false)
                .HasMaxLength(2048);

            builder.Property(page => page.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(page => page.PageAccessToken)
                .IsRequired(false)
                .HasMaxLength(4096);

            builder.Property(page => page.IntegrationStatus)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(page => page.IsActive)
                .IsRequired();

            builder.Property(page => page.TokenExpiresAt)
                .IsRequired(false);

            builder.Property(page => page.LastSyncedAt)
                .IsRequired(false);

            builder.Property(page => page.CreatedAt)
                .IsRequired();

            builder.Property(page => page.UpdatedAt)
                .IsRequired(false);

            builder.HasOne(page => page.User)
                .WithMany(user => user.FacebookPages)
                .HasForeignKey(page => page.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
