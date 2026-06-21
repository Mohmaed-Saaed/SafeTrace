using SafeTrace.Domain.Specifications;
using SafeTrace.Infrastructure.DataAccess;
using SafeTrace.Infrastructure.Specifications;

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

        private IQueryable<T> ApplySpecification(ISpecification<T> spec)
        {
            return SpecificationEvaluator.GetQuery(_db.AsQueryable(), spec);
        }

        public async Task<T?> GetAsync(ISpecification<T> spec)
        {
            return await ApplySpecification(spec).FirstOrDefaultAsync();
        }
        public async Task<IReadOnlyList<T>> GetAllAsync(ISpecification<T> spec)
        {
            return await ApplySpecification(spec).ToListAsync();
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

        public async Task<int> CountAsync()
        {
            return await CountAsync();
        }

        public async Task<int> CountAsync(ISpecification<T> spec)
        {
            return await ApplySpecification(spec).CountAsync();
        }

        public async Task<bool> AnyAsync()
        {
            return await AnyAsync();
        }
        
        public async Task<bool> AnyAsync(ISpecification<T> spec)
        {
            return await ApplySpecification(spec).AnyAsync();
        }

        public async Task<bool> AnyAsync()
        {
            return await AnyAsync();
        }
    }
}