using Microsoft.EntityFrameworkCore.Storage;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using SafeTrace.Infrastructure.DataAccess;
using SafeTrace.Infrastructure.Repositories.Repository;

namespace SafeTrace.Infrastructure.Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private readonly Dictionary<Type, object> _repositories = new();
        private IDbContextTransaction? _transaction;

        public IRepository<T> Repository<T>() where T : class
        {
            var type = typeof(T);

            if (_repositories.ContainsKey(type))
                return (IRepository<T>)_repositories[type];

            var repo = new Repository<T>(_context);

            _repositories[type] = repo;

            return repo;
        }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction is null)
            {
                _transaction = await _context.Database.BeginTransactionAsync();
            }
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync();

                if (_transaction is not null)
                {
                    await _transaction.CommitAsync();
                    await _transaction.DisposeAsync();

                    _transaction = null;
                }
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction is not null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();

                _transaction = null;
            }
        }

        public void Dispose()
        {
            try
            {
                _transaction?.Dispose();
            }
            catch { }

            try
            {
                _context.Dispose();
            }
            catch { }
        }
    }
}
