using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafeTrace.Domain.Entities;

namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    internal class ChatConfiguration : IEntityTypeConfiguration<Chat>
    {
        public void Configure(EntityTypeBuilder<Chat> builder)
        {
            builder.ToTable("Chats");

            builder.HasKey(c => c.Id);

            builder.HasOne(c => c.Case)
                .WithMany(b => b.Chats)
                .HasForeignKey(c => c.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Messages)
                .WithOne(m => m.Chat)
                .HasForeignKey(m => m.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}