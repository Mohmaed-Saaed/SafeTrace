using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
    {
        public void Configure(EntityTypeBuilder<Complaint> builder)
        {
            builder.ToTable("Complaints");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Message).IsRequired().HasMaxLength(2000);

            builder.HasOne(c => c.User)
                .WithMany(u => u.Complaints)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.Property(c => c.SolutionMessage).HasMaxLength(2000);

            builder.Property(c => c.ComplaintStatus)
                .HasConversion<int>();
 
        }
    }
}