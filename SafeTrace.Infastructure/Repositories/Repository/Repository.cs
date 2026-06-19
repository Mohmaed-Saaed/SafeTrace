using System.Linq.Expressions;
using Microsoft.AspNetCore.SignalR;
using SafeTrace.Domain.Common;
using SafeTrace.Infrastructure.DataAccess;

namespace SafeTrace.Infrastructure.Repositories.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;

        private DbSet<T> _db { set; get; }
        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _db = _context.Set<T>();
        }
        public async Task<bool> CreateAsync(T entity)
        {
            try
            {
                await _db.AddAsync(entity);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ex: {ex}");
                return false;
            }
        }

        public async Task<bool> CreateRangeAsync(IEnumerable<T> entity)
        {
            try
            {
                await _db.AddRangeAsync(entity);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ex: {ex}");
                return false;
            }
        }

        public Task<bool> UpdateAsync(T entity)
        {
            try
            {
                _db.Update(entity);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ex: {ex}");
                return Task.FromResult(false);
            }
        }

        public Task<bool> DeleteAsync(T entity)
        {
            try
            {
                _db.Attach(entity);
                _db.Remove(entity);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ex: {ex}");
                return Task.FromResult(false);
            }
        }

        public Task<bool> DeleteRangeAsync(IEnumerable<T> entity)
        {
            try
            {
                _db.RemoveRange(entity);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ex: {ex}");
                return Task.FromResult(false);
            }
        }
        public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? expression = null, bool tracked = true,
            Expression<Func<T, object>>? orderBy = null, string orderByDirection = OrderBy.Ascending, int page = 1, int pageSize = 10, params Expression<Func<T, object>>[] includes)
        {

            IQueryable<T> entities = _db;

            if (!tracked)
                entities = entities.AsNoTracking();

            if (includes is not null && includes.Length > 0)
            {
                foreach (var item in includes)
                    entities = entities.Include(item);
            }

            if (expression is not null)
                entities = entities.Where(expression);

            if (orderBy != null)
            {
                entities = orderByDirection == OrderBy.Descending
                    ? entities.OrderByDescending(orderBy)
                    : entities.OrderBy(orderBy);
            }

            if (page > 0 && pageSize > 0)
            {
                entities = entities
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);
            }

            return await entities.ToListAsync();
        }

        public async Task<T?> GetOneAsync(Expression<Func<T, bool>>? expression = null, bool tracked = true, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _db;

            if (!tracked)
            {
                query = query.AsNoTracking();
            }

            if (includes is not null && includes.Length > 0)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            if (expression is not null)
            {
                query = query.Where(expression);
            }

            return await query.SingleOrDefaultAsync();
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>>? condition = null)
        {
            if (condition is not null)
                return await _db.AnyAsync(condition);

            return await _db.AnyAsync();
        }
        public async Task<int> CountAsync(Expression<Func<T, bool>>? expression = null)
        {
            IQueryable<T> entities = _db;

            if (expression is not null)
            {
                return await entities.Where(expression).CountAsync();
            }

            return await entities.CountAsync();
        }
        public async Task AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
        }
    }
}
