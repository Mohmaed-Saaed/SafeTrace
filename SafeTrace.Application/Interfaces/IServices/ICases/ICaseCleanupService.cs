namespace SafeTrace.Application.Interfaces.IServices.ICases;

public interface ICaseCleanupService
{
    Task CleanupExpiredUrgentCasesAsync();
}
