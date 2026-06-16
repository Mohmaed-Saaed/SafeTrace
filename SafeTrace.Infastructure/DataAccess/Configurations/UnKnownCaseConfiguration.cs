using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class UnKnownCaseConfiguration : IEntityTypeConfiguration<UnknownCase>
    {
       
            public void Configure(EntityTypeBuilder<UnknownCase> builder)
            {
                builder.ToTable("LongTermMissingCases");

                builder.Property(l => l.Government)
                    .IsRequired()
                    .HasMaxLength(200);

                builder.Property(l => l.City)
                    .IsRequired()
                    .HasMaxLength(200);

                builder.Property(l => l.Street)
                    .HasMaxLength(500);
            
        }
    }
}
