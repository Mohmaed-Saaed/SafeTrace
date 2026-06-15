using SafeTrace.Domain.Interfaces.IRepositories;
using SafeTrace.Infrastructure.DataAccess;
using SafeTrace.Infrastructure.Repositories.Repository;

namespace SafeTrace.Infrastructure.Repositories.Repositories
{
    public class BaseCaseRepository : Repository<BaseCase>, IBaseCaseRepository
    {
        public BaseCaseRepository(ApplicationDbContext context) : base(context)
        {
        }

        
    }

}
