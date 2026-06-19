using NetTopologySuite;
using NetTopologySuite.Geometries;
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

        public async Task<IEnumerable<UrgentCase>> GetNearestAsync(double latitude,double longitude, int take = 10)
        {
            var factory = NtsGeometryServices.Instance.CreateGeometryFactory(4326);

            var userLocation = factory.CreatePoint(new Coordinate(longitude, latitude));

            return await _context.UrgentCases
                .AsNoTracking()
                .Include(c => c.Photos)
                .Where(c => c.Location != null)
                .OrderBy(c => c.Location.Distance(userLocation))
                .ThenBy(c => c.CreatedAt)
                .Take(take)
                .ToListAsync();
        }
    }
}
