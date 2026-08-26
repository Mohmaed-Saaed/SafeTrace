using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class FacebookImportedPostFileConfiguration
        : IEntityTypeConfiguration<FacebookImportedPostFile>
    {
        public void Configure(EntityTypeBuilder<FacebookImportedPostFile> builder)
        {
            builder.ToTable("FacebookImportedPostFiles");

            builder.HasKey(file => file.Id);

            builder.Property(file => file.FacebookMediaId)
                .IsRequired(false)
                .HasMaxLength(100);

            builder.Property(file => file.FileUrl)
                .IsRequired()
                .HasMaxLength(2048);

            builder.Property(file => file.CreatedAt)
                .IsRequired();

            builder.HasIndex(file => file.FacebookImportedPostId);
        }
    }
}
