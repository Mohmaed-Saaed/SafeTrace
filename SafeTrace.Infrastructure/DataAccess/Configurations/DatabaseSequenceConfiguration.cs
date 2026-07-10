namespace SafeTrace.Infrastructure.DataAccess.Configurations
{
    public class DatabaseConfiguration
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.HasSequence<int>("LongTermCaseSequence")
                .StartsAt(1000)
                .IncrementsBy(1);

            modelBuilder.HasSequence<int>("UrgentCaseSequence")
                .StartsAt(1000)
                .IncrementsBy(1);

            modelBuilder.HasSequence<int>("UnknownCaseSequence")
                .StartsAt(1000)
                .IncrementsBy(1);
        }
    }
}


