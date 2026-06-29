//using Microsoft.EntityFrameworkCore.Metadata.Builders;

//namespace SafeTrace.Infrastructure.DataAccess.Configurations
//{
//    public class AiSearchUsageConfiguration  : IEntityTypeConfiguration<AiSearchUsage>
//    {
//        public void Configure(EntityTypeBuilder<AiSearchUsage> builder)
//        {
//            builder.ToTable("AiSearchUsages");

//            builder.HasKey(x => x.Id);

//            builder.Property(x => x.CreatedAt)
//                .IsRequired();

//            builder.Property(x => x.UserId)
//                .IsRequired();

//            builder.HasOne(x => x.User)
//                .WithMany(x => x.AiSearchUsages)
//                .HasForeignKey(x => x.UserId)
//                .OnDelete(DeleteBehavior.Restrict);
//        }
//    }
//}
