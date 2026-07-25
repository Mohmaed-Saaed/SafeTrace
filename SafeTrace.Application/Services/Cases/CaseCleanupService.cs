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
            var now = DateTime.UtcNow;
            const int batchSize = 500; 
            int updatedRows;

            do
            {
                updatedRows = await _unitOfWork
                    .Repository<UrgentCase>()
                    .Query()
                    .Where(x => x.Status == CaseStatus.Active && x.EndDate <= now)
                    .Take(batchSize)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(u => u.Status, CaseStatus.Expired)
                        .SetProperty(u => u.PreviousStatus, CaseStatus.Active)
                        .SetProperty(u => u.UpdatedAt, now)
                    );

            } while (updatedRows == batchSize);
        }
    }   
}