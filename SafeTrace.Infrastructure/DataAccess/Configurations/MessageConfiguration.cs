using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.ToTable("Messages");

            builder.Property(x => x.FileType)
                 .HasConversion<string>();

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Content).IsRequired().HasMaxLength(2000);

            builder.HasOne(m => m.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}