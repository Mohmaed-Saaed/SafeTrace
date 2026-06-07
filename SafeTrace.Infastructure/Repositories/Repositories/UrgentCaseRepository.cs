using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Interfaces.IRepositories;
using SafeTrace.Infrastructure.DataAccess;
using SafeTrace.Infrastructure.Repositories.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Infrastructure.Repositories.Repositories
{
    public class UrgentCaseRepository : Repository<UrgentCase>, IUrgentCaseRepository
    {
        public UrgentCaseRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
