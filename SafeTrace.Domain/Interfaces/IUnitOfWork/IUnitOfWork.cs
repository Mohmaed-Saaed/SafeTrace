namespace SafeTrace.Domain.Interfaces.IUnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        public IRepository<RefreshToken> RefreshTokenRepository { get;}
        public IRepository<UserOtp> UserOtpRepository { get;}
        //public IRepository<UrgentCase> UrgentCaseRepository { get;}
        //public IRepository<LongTermMissingCase> LongTermCaseRepository { get;}
        //public IRepository<UnknownCase> UnknownCaseRepository { get;}
        public IRepository<Case> CaseRepository { get;}
        public IRepository<CasePhoto> CasePhotoRepository { get;}
        public IRepository<Chat> ChatRepository { get;}
        public IRepository<Complaint> ComplaintRepository { get;}
        public IRepository<FoundPersonInfo> FoundPersonInfoRepository { get;}
        public IRepository<Message> MessageRepository { get;}
        public IRepository<Notification> NotificationRepository { get;}
        public IRepository<AgeCategory> AgeCategoryRepository { get;}

        Task<int> SaveAsync();

        Task BeginTransactionAsync();

        Task CommitTransactionAsync();

        Task RollbackTransactionAsync();
    }
}
