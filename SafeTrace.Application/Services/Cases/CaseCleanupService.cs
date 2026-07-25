using SafeTrace.Application.Interfaces.IServices.ICases;

namespace SafeTrace.Application.Services.Cases
{
    public class CaseCleanupService : ICaseCleanupService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CaseCleanupService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task CleanupExpiredUrgentCasesAsync()
        {
            var expirationDate = DateTime.UtcNow.AddHours(-48);
            const int batchSize = 500; 
            int updatedRows;

            do
            {
                updatedRows = await _unitOfWork
                    .Repository<UrgentCase>()
                    .Query()
                    .Where(x => x.Status == CaseStatus.Active && x.CreatedAt <= expirationDate)
                    .Take(batchSize)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(u => u.Status, CaseStatus.Expired)
                        .SetProperty(u => u.PreviousStatus, CaseStatus.Active)
                        .SetProperty(u => u.DeletedAt, DateTime.UtcNow)
                    );

            } while (updatedRows == batchSize);
        }
    }   
}