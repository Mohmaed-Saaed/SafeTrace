using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Interfaces.IRepositories;
using SafeTrace.Infrastructure.DataAccess;
using SafeTrace.Infrastructure.Repositories.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Infrastructure.Repositories.Repositories
{
    public class UnknownCaseRepository : Repository<UnknownCase>, IUnknownCaseRepository
    {
        public UnknownCaseRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
