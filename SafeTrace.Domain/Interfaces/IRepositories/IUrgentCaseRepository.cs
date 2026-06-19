namespace SafeTrace.Domain.Interfaces.IRepositories
{
    public interface IUrgentCaseRepository : IRepository<UrgentCase>
    {
        Task<IEnumerable<UrgentCase>> GetNearestAsync(double latitude,double longitude, int take = 10);
    }
}
