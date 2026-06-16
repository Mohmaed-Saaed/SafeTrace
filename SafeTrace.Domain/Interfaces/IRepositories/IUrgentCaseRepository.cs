namespace SafeTrace.Domain.Interfaces.IRepositories
{
    public interface IUrgentCaseRepository : IRepository<UrgentCase>
    {
        Task<IEnumerable<UrgentCase>> GetNearestCasesAsync(
            long currentCaseId,
            double latitude,
            double longitude,
            double radiusKm = 50,
            int take = 5);
    }
}
