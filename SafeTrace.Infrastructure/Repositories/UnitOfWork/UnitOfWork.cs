using System.Data;
using Microsoft.EntityFrameworkCore.Storage;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using SafeTrace.Infrastructure.DataAccess;
using SafeTrace.Infrastructure.Repositories.Repository;

namespace SafeTrace.Infrastructure.Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork, IAsyncDisposable
    {
        private readonly ApplicationDbContext _context;

        private IDbContextTransaction? _currentTransaction;

        private readonly Dictionary<Type, object> _repositories = new();

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IRepository<TEntity> Repository<TEntity>() where TEntity : class
        {
            if (_repositories.TryGetValue(typeof(TEntity), out var repository))
                return (IRepository<TEntity>)repository;

            var repo = new Repository<TEntity>(_context);

            _repositories.Add(typeof(TEntity), repo);

            return repo;
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            if (_currentTransaction is not null)
            {
                throw new InvalidOperationException("A transaction is already in progress.");
            }

            _currentTransaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_currentTransaction is null)
            {
                throw new InvalidOperationException("No transaction is currently in progress.");
            }

            try
            {
                await _currentTransaction.CommitAsync();
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_currentTransaction is null)
                return;

            try
            {
                await _currentTransaction.RollbackAsync();
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
        private static readonly HashSet<string> AllowedSequenceNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "LongTermCaseSequence",
            "UrgentCaseSequence",
            "UnknownCaseSequence"
        };

        public async Task<int> GetNextSequenceValueAsync(string sequenceName)
        {
            if (string.IsNullOrWhiteSpace(sequenceName) || !AllowedSequenceNames.Contains(sequenceName))
            {
                throw new ArgumentException("اسم التسلسل غير صالج.", nameof(sequenceName));
            }

            var connection = _context.Database.GetDbConnection();

            var shouldCloseConnection = connection.State != ConnectionState.Open;

            if (shouldCloseConnection)
                await connection.OpenAsync();

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = $"SELECT NEXT VALUE FOR [{sequenceName}]";

                var result = await command.ExecuteScalarAsync();

                return Convert.ToInt32(result);
            }
            finally
            {
                if (shouldCloseConnection)
                    await connection.CloseAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_currentTransaction is not null)
            {
                await _currentTransaction.DisposeAsync();
            }

            await _context.DisposeAsync();
        }
    }
}