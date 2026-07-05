using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class CaseFileConfiguration : IEntityTypeConfiguration<CaseFile>
    {
        public void Configure(EntityTypeBuilder<CaseFile> builder)
        {
            builder.ToTable("CaseFiles");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.ImagePath)
                .IsRequired()
                .HasMaxLength(1000);

            builder.HasOne(p => p.Case)
                .WithMany(c => c.CaseFiles)
                .HasForeignKey(p => p.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}