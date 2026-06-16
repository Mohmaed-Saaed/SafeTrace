using SafeTrace.Application.Helpers;
using SafeTrace.Domain.Interfaces.IRepositories;
using SafeTrace.Infrastructure.DataAccess;
using SafeTrace.Infrastructure.Repositories.Repository;

namespace SafeTrace.Infrastructure.Repositories.Repositories
{
    public class UrgentCaseRepository : Repository<UrgentCase>, IUrgentCaseRepository
    {
        private readonly ApplicationDbContext _context;

        public UrgentCaseRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UrgentCase>> GetNearestCasesAsync(
            long currentCaseId,
            double latitude,
            double longitude,
            double radiusKm = 50,
            int take = 5)
        {
            var latRange = radiusKm / 111.0;

            var lonRange = radiusKm / (111.0 * Math.Cos(latitude * Math.PI / 180));

            var candidates = await _context.UrgentCases
                .AsNoTracking()
                .Where(c =>
                    c.Id != currentCaseId &&
                    c.LocationLatitude >= latitude - latRange &&
                    c.LocationLatitude <= latitude + latRange &&
                    c.LocationLongitude >= longitude - lonRange &&
                    c.LocationLongitude <= longitude + lonRange)
                .Include(c => c.Photos)
                .ToListAsync();

            return [.. candidates
                .OrderBy(c =>
                    GeoHelper.DistanceKm(
                        latitude,
                        longitude,
                        c.LocationLatitude,
                        c.LocationLongitude))
                .ThenBy(c => c.CreatedAt)
                .Take(take)];
        }
    }
}
