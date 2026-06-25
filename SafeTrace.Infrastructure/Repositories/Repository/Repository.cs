using SafeTrace.Domain.Common;
using SafeTrace.Infrastructure.DataAccess;
using System.Linq.Expressions;

namespace SafeTrace.Infrastructure.Repositories.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<T> _db;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _db = _context.Set<T>();
        }

        public IQueryable<T> Query(
            bool tracked = true,
            Expression<Func<T, object>>? orderBy = null,
            string orderByDirection = OrderBy.Ascending,
            int? page = null,
            int? pageSize = null,
            params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _db;

            if (!tracked)
                query = query.AsNoTracking();

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            if (orderBy is not null)
            {
                query = orderByDirection == OrderBy.Descending
                    ? query.OrderByDescending(orderBy)
                    : query.OrderBy(orderBy);
            }

            if (page.HasValue && pageSize.HasValue)
            {
                query = query
                    .Skip((page.Value - 1) * pageSize.Value)
                    .Take(pageSize.Value);
            }

            return query;
        }

        public async Task<T?> GetOneAsync(Expression<Func<T, bool>> predicate, bool tracked = true, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _db;

            if (!tracked)
                query = query.AsNoTracking();

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.FirstOrDefaultAsync(predicate);
        }

        public async Task<T?> GetByIdAsync(object id)
        {
            return await _db.FindAsync(id);
        }

        public async Task CreateAsync(T entity)
        {
            await _db.AddAsync(entity);
        }

        public async Task CreateRangeAsync(IEnumerable<T> entities)
        {
            await _db.AddRangeAsync(entities);
        }

        public void Update(T entity)
        {
            _db.Update(entity);
        }

        public void Remove(T entity)
        {
            _db.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            _db.RemoveRange(entities);
        }

        public async Task<bool> AnyAsync()
        {
            return await _db.AnyAsync();
        }

        public async Task<int> CountAsync()
        {
            return await _db.CountAsync();
        }
        
        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return await _db.CountAsync(predicate);
        }
    }
}