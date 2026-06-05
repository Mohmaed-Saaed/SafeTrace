using SafeTrace.Domain.Interfaces.IReposityory;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Domain.Interfaces.IUnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<T> Repository<T>() where T : class;

        Task<int> SaveAsync();

        Task BeginTransactionAsync();

        Task CommitTransactionAsync();

        Task RollbackTransactionAsync();
    }
}
