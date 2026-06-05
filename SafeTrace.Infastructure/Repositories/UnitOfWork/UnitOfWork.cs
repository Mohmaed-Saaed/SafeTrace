using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SafeTrace.Domain.Interfaces.IReposityory;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using SafeTrace.Infrastructure.DataAccess;
using SafeTrace.Infrastructure.Repositories.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Infrastructure.Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        private IDbContextTransaction? _transaction;

        private readonly Dictionary<Type, object> _repositories = new();
        private readonly object _reposLock = new();

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IRepository<T> Repository<T>() where T : class
        {
            var type = typeof(T);
            if (_repositories.TryGetValue(type, out var repo))
            {
                return (IRepository<T>)repo!;
            }

            lock (_reposLock)
            {
                if (_repositories.TryGetValue(type, out repo))
                {
                    return (IRepository<T>)repo!;
                }

                var repository = new Repository<T>(_context);
                _repositories[type] = repository;
                return repository;
            }
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
