using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class DonationConfiguration : IEntityTypeConfiguration<Donation>
    {
        public void Configure(EntityTypeBuilder<Donation> builder)
        {
            builder.ToTable("Donations");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Amount)
                .IsRequired();

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(x => x.Message)
                .HasMaxLength(1000);

            builder.Property(x => x.PaymentStatus)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();
        }
    }
}
