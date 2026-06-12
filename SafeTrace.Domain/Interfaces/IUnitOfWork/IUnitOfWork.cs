using SafeTrace.Domain.Interfaces.IRepositories;

namespace SafeTrace.Domain.Interfaces.IUnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IUserSessionRepository UserSessionRepository { get; }

        IBaseCaseRepository BaseCaseRepository { get; }
        ICasePhotoRepository CasePhotoRepository { get; }
        IChatRepository ChatRepository { get; }
        IComplaintRepository ComplaintRepository { get; }
        IFoundPersonInfoRepository FoundPersonInfoRepository { get; }
        ILongTermMissingCaseRepository LongTermMissingCaseRepository { get; }
        IMessageRepository MessageRepository { get; }
        INotificationRepository NotificationRepository { get; }
        IUnknownCaseRepository UnknownCaseRepository { get; }
        IUrgentCaseRepository UrgentCaseRepository { get; }

        Task<int> SaveAsync();

        Task BeginTransactionAsync();

        Task CommitTransactionAsync();

        Task RollbackTransactionAsync();
    }
}
