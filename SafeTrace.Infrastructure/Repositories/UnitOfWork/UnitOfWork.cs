using Microsoft.EntityFrameworkCore.Storage;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using SafeTrace.Infrastructure.DataAccess;
using SafeTrace.Infrastructure.Repositories.Repository;

namespace SafeTrace.Infrastructure.Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        private IDbContextTransaction? _transaction;

        public IRepository<RefreshToken> RefreshTokenRepository { get; private set; }
        public IRepository<UserOtp> UserOtpRepository { get; private set; }
       // public IRepository<UrgentCase> UrgentCaseRepository { get; private set; }
       // public IRepository<LongTermMissingCase> LongTermCaseRepository { get; private set; }
       // public IRepository<UnknownCase> UnknownCaseRepository { get; private set; }
        public IRepository<Case> CaseRepository { get; private set; }
        public IRepository<CasePhoto> CasePhotoRepository { get; private set; }
        public IRepository<Chat> ChatRepository { get; private set; }
        public IRepository<Complaint> ComplaintRepository { get; private set; }
        public IRepository<FoundPersonInfo> FoundPersonInfoRepository { get; private set; }
        public IRepository<Message> MessageRepository { get; private set; }
        public IRepository<Notification> NotificationRepository { get; private set; }
        public IRepository<AgeCategory> AgeCategoryRepository { get; private set; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            //UrgentCaseRepository = new Repository<UrgentCase>(_context);
            //UnknownCaseRepository = new Repository<UnknownCase>(_context);
            //LongTermCaseRepository = new Repository<LongTermMissingCase>(_context);
            CaseRepository = new Repository<Case>(_context);
            CasePhotoRepository = new Repository<CasePhoto>(_context);
            ChatRepository = new Repository<Chat>(_context);
            ComplaintRepository = new Repository<Complaint>(_context);
            FoundPersonInfoRepository = new Repository<FoundPersonInfo>(_context);
            MessageRepository = new Repository<Message>(_context);
            NotificationRepository = new Repository<Notification>(_context);
            UserOtpRepository = new Repository<UserOtp>(_context);
            RefreshTokenRepository = new Repository<RefreshToken>(_context);
            AgeCategoryRepository = new Repository<AgeCategory>(_context);
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
