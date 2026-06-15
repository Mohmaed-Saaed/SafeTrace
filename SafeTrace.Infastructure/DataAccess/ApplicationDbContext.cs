namespace SafeTrace.Infrastructure.DataAccess
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<BaseCase> BaseCase { get; set; }
        public DbSet<LongTermMissingCase> LongTermMissingCases{ get; set; }
        public DbSet<UrgentCase> UrgentCases { get; set; }
     

        public DbSet<FoundPersonInfo> FoundPersonInfos { get; set; }
        public DbSet<UnknownCase> UnknownCases { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<CasePhoto> CasePhotos { get; set; }
        public DbSet<Chat> Chats{ get; set; }
        public DbSet<Message> Messages{ get; set; }
        public DbSet<Notification> Notifications{ get; set; }
        public DbSet<AgeCategory> AgeCategories { get; set; }


        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }

    }
}
