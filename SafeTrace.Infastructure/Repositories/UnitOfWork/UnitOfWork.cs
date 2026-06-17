using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SafeTrace.Domain.Interfaces.IRepositories;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using SafeTrace.Infrastructure.DataAccess;
using SafeTrace.Infrastructure.Repositories.Repositories;

namespace SafeTrace.Infrastructure.Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        private IDbContextTransaction? _transaction;

        public IRefreshTokenRepository RefreshTokenRepository { get; set; }
        public IUserOtpRepository UserOtpRepository { get; set; }
        public IBaseCaseRepository BaseCaseRepository { get; private set; }
        public ICasePhotoRepository CasePhotoRepository { get; private set; }
        public IChatRepository ChatRepository { get; private set; }
        public IComplaintRepository ComplaintRepository { get; private set; }
        public IFoundPersonInfoRepository FoundPersonInfoRepository { get; private set; }
        public ILongTermMissingCaseRepository LongTermMissingCaseRepository { get; private set; }
        public IMessageRepository MessageRepository { get; private set; }
        public INotificationRepository NotificationRepository { get; private set; }
        public IUnknownCaseRepository UnknownCaseRepository { get; private set; }
        public IUrgentCaseRepository UrgentCaseRepository { get; private set; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            RefreshTokenRepository = new RefreshTokenRepository(_context);
            UserOtpRepository = new UserOtpRepository(_context);
            BaseCaseRepository = new BaseCaseRepository(_context);
            CasePhotoRepository = new CasePhotoRepository(_context);
            ChatRepository = new ChatRepository(_context);
            ComplaintRepository = new ComplaintRepository(_context);
            FoundPersonInfoRepository = new FoundPersonInfoRepository(_context);
            LongTermMissingCaseRepository = new LongTermMissingCaseRepository(_context);
            MessageRepository = new MessageRepository(_context);
            NotificationRepository = new NotificationRepository(_context);
            UnknownCaseRepository = new UnknownCaseRepository(_context);
            UrgentCaseRepository = new UrgentCaseRepository(_context);
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
