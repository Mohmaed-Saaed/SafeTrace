using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    public class AgeCategoryConfiguration : IEntityTypeConfiguration<AgeCategory>
    {
        public void Configure(EntityTypeBuilder<AgeCategory> builder)
        {
            builder.ToTable("AgeCategories");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.MinAge)
                .IsRequired();

            builder.Property(x => x.MaxAge)
                .IsRequired();

            builder.HasMany(x => x.BaseCases)
                .WithOne(x => x.AgeCategory)
                .HasForeignKey(x => x.AgeCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }


}
