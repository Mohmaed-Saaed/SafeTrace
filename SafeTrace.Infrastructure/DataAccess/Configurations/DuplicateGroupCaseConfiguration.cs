using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class DuplicateGroupCaseConfiguration : IEntityTypeConfiguration<DuplicateGroupCase>
    {
        public void Configure(EntityTypeBuilder<DuplicateGroupCase> builder)
        {

            builder.ToTable("DuplicateGroupCases");

            builder.HasKey(dgc => dgc.Id);

            builder.Property(c => c.MatchedBy)
            .HasConversion<string>()
            .HasMaxLength(10);

            builder.HasOne(dgc => dgc.DuplicateGroup)
                   .WithMany(dg => dg.DuplicateCases)
                   .HasForeignKey(dgc => dgc.DuplicateGroupId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(dgc => dgc.Case)
                   .WithMany(c => c.DuplicateGroups)
                   .HasForeignKey(dgc => dgc.CaseId);
        }
    }
}
