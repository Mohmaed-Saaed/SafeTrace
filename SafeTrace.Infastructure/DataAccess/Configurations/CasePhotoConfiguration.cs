using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class CasePhotoConfiguration : IEntityTypeConfiguration<CasePhoto>
    {
        public void Configure(EntityTypeBuilder<CasePhoto> builder)
        {
            builder.ToTable("CasePhotos");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.ImagePath)
                .IsRequired()
                .HasMaxLength(1000);

            builder.HasOne(p => p.Case)
                .WithMany(c => c.Photos)
                .HasForeignKey(p => p.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}