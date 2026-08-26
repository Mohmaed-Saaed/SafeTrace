using NetTopologySuite.Geometries;
using SafeTrace.Infrastructure.DataAccess.Configurations;
using Audit.EntityFramework;

namespace SafeTrace.Infrastructure.DataAccess
{
    public class ApplicationDbContext : AuditIdentityDbContext<ApplicationUser>
    {
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Case> Cases { get; set; }
        public DbSet<DuplicateGroup> DuplicateGroups { get; set; }
        public DbSet<DuplicateGroupCase> DuplicateGroupCases { get; set; }
        public DbSet<FoundPersonInfo> FoundPersonInfos { get; set; }
        public DbSet<AiSearchUsage> AiSearchUsages { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<CaseFile> CaseFiles { get; set; }
        public DbSet<Chat> Chats{ get; set; }
        public DbSet<Message> Messages{ get; set; }
        public DbSet<Notification> Notifications{ get; set; }
        public DbSet<AgeCategory> AgeCategories { get; set; }
        public DbSet<UserOtp> UserOtps { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Donation> Donations { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<FacebookPage> FacebookPages { get; set; }
        public DbSet<FacebookImportedPost> FacebookImportedPosts { get; set; }
        public DbSet<FacebookImportedPostFile> FacebookImportedPostFiles { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
            
            DatabaseConfiguration.Configure(builder);
        }
    }
}
