using SafeTrace.Domain.Interfaces.IRepositories;
using SafeTrace.Infrastructure.DataAccess;
using SafeTrace.Infrastructure.Repositories.Repository;

namespace SafeTrace.Infrastructure.Repositories.Repositories
{
    public class UserOtpRepository : Repository<UserOtp>, IUserOtpRepository
    {
        public UserOtpRepository(ApplicationDbContext context) : base(context)
        {
            
        }
    }
}
