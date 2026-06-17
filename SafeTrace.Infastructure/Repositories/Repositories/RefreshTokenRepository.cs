using SafeTrace.Domain.Interfaces.IRepositories;
using SafeTrace.Infrastructure.DataAccess;
using SafeTrace.Infrastructure.Repositories.Repository;

namespace SafeTrace.Infrastructure.Repositories.Repositories
{
    public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}