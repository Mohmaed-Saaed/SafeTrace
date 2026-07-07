using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class DuplicateGroupConfiguration : IEntityTypeConfiguration<DuplicateGroup>
    {
        public void Configure(EntityTypeBuilder<DuplicateGroup> builder)
        {
            builder.ToTable("DuplicateGroups");

            builder.HasKey(dg => dg.Id);

            builder.Property(c => c.GroupStatus)
           .HasConversion<string>()
           .HasMaxLength(10);

            builder.HasMany(dg => dg.DuplicateCases)
                   .WithOne(dgc => dgc.DuplicateGroup)
                   .HasForeignKey(dgc => dgc.DuplicateGroupId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
