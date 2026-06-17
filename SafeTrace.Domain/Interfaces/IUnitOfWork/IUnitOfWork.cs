using SafeTrace.Domain.Interfaces.IRepositories;

namespace SafeTrace.Domain.Interfaces.IUnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        public IRefreshTokenRepository RefreshTokenRepository { get; set; }
        public IUserOtpRepository UserOtpRepository { get; set; }
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
