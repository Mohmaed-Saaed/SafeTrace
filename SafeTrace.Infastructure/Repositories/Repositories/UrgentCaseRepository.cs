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
    }
}
