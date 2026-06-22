using SafeTrace.Infrastructure.DataAccess;


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

        public async Task<bool> AnyAsync()
        {
            return await AnyAsync();
        }
    }
}