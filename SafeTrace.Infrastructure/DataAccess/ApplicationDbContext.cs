namespace SafeTrace.Infrastructure.DataAccess
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
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

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);


            //builder.Entity<BaseCase>().ToTable("BaseCases");

            //builder.Entity<UnknownCase>().ToTable("UnknownCases");

            //builder.Entity<LongTermMissingCase>().ToTable("LongTermMissingCases");

            //builder.Entity<UrgentCase>().ToTable("UrgentCases");

        }

    }
}
