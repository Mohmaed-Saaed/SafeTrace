
namespace SafeTrace.Domain.Interfaces.IRepositories
{
    public interface IChatRepository : IRepository<Chat>
    {
        Task<Chat?> GetExistingChatAsync(long caseId, string userId1, string userId2);
        Task<IEnumerable<Chat>> GetUserChatsAsync(string userId);
        Task<Chat?> GetChatWithDetailsAsync(long chatId);
    }
}
