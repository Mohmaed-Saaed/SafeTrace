using SafeTrace.Domain.Interfaces.IRepositories;

namespace SafeTrace.Domain.Interfaces.IUnitOfWork
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IChatRepository ChatRepository { get; }
        IMessageRepository MessageRepository { get; }

        IRepository<TEntity> Repository<TEntity>() where TEntity : class;

        Task<int> SaveAsync();

        Task BeginTransactionAsync();

        Task CommitTransactionAsync();

        Task RollbackTransactionAsync();
    }
}
