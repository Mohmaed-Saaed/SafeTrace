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


            var expiredCases = await _unitOfWork
                .Repository<UrgentCase>()
                .Query()
                .Where(x => x.Status == CaseStatus.Active && x.CreatedAt <= expirationDate)
                .ToListAsync();

            if (expiredCases.Count == 0)
                return;

            foreach (var urgentCase in expiredCases)
            {
                urgentCase.Status = CaseStatus.Expired;
                urgentCase.PreviousStatus = CaseStatus.Active;
                urgentCase.DeletedAt = DateTime.UtcNow;

                _unitOfWork.Repository<UrgentCase>().Update(urgentCase);
            }
            await _unitOfWork.SaveAsync();
        }
    }   
}