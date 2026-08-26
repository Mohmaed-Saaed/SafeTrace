using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class FacebookImportedPostConfiguration
        : IEntityTypeConfiguration<FacebookImportedPost>
    {
        public void Configure(EntityTypeBuilder<FacebookImportedPost> builder)
        {
            builder.ToTable("FacebookImportedPosts", table =>
            {
                table.HasCheckConstraint(
                    "CK_FacebookImportedPosts_Confidence",
                    "[Confidence] IS NULL OR ([Confidence] >= 0 AND [Confidence] <= 1)");

                table.HasCheckConstraint(
                    "CK_FacebookImportedPosts_Age",
                    "[Age] IS NULL OR ([Age] >= 0 AND [Age] <= 120)");

                table.HasCheckConstraint(
                    "CK_FacebookImportedPosts_CaseReferences",
                    "[CaseId] IS NULL OR [DuplicateCaseId] IS NULL");
            });

            builder.HasKey(post => post.Id);

            builder.Property(post => post.FacebookPostId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(post => post.PostText)
                .IsRequired(false)
                .HasColumnType("nvarchar(max)");

            builder.Property(post => post.PostUrl)
                .IsRequired(false)
                .HasMaxLength(2048);

            builder.Property(post => post.Classification)
                .HasConversion<string>()
                .HasMaxLength(30);

            builder.Property(post => post.Gender)
                .HasConversion<string>()
                .HasMaxLength(10);

            builder.Property(post => post.Relation)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(post => post.LocationAccuracy)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(post => post.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(post => post.FName).HasMaxLength(60);
            builder.Property(post => post.SName).HasMaxLength(60);
            builder.Property(post => post.TName).HasMaxLength(60);
            builder.Property(post => post.LName).HasMaxLength(60);
            builder.Property(post => post.Government).HasMaxLength(100);
            builder.Property(post => post.City).HasMaxLength(100);
            builder.Property(post => post.Street).HasMaxLength(200);
            builder.Property(post => post.CommunicationPhone).HasMaxLength(30);
            builder.Property(post => post.Description).HasMaxLength(2000);
            builder.Property(post => post.ReviewNotes).HasMaxLength(2000);

            builder.Property(post => post.CreatedAt)
                .IsRequired();

            builder.Property(post => post.RowVersion)
                .IsRowVersion();

            builder.HasIndex(post => new
                {
                    post.FacebookPageId,
                    post.FacebookPostId
                })
                .IsUnique();

            builder.HasIndex(post => new
            {
                post.Status,
                post.Id
            });

            builder.HasIndex(post => post.CaseId)
                .IsUnique()
                .HasFilter("[CaseId] IS NOT NULL");

            builder.HasIndex(post => post.DuplicateCaseId);

            builder.HasOne(post => post.FacebookPage)
                .WithMany(page => page.ImportedPosts)
                .HasForeignKey(post => post.FacebookPageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(post => post.Files)
                .WithOne(file => file.FacebookImportedPost)
                .HasForeignKey(file => file.FacebookImportedPostId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(post => post.Case)
                .WithMany()
                .HasForeignKey(post => post.CaseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(post => post.DuplicateCase)
                .WithMany()
                .HasForeignKey(post => post.DuplicateCaseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
