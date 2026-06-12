using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
    {
        public void Configure(EntityTypeBuilder<UserSession> builder)
        {
            builder.HasKey(s => new { s.UserId, s.JwtId });

            builder.Property(s => s.JwtId)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(s => s.IsRevoked)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(s => s.ExpiresAtUtc)
                .IsRequired();

            builder.HasOne(s => s.User)              
                .WithMany()                           
                .HasForeignKey(s => s.UserId)        
                .OnDelete(DeleteBehavior.Cascade)  
                .IsRequired();                      
        }
    }
}