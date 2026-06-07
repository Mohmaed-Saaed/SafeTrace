using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Interfaces.IRepositories;
using SafeTrace.Infrastructure.DataAccess;
using SafeTrace.Infrastructure.Repositories.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Infrastructure.Repositories.Repositories
{
    public class FoundPersonInfoRepository : Repository<FoundPersonInfo>, IFoundPersonInfoRepository
    {
        public FoundPersonInfoRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
