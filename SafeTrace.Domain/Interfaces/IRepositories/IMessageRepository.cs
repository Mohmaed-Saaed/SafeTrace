namespace SafeTrace.Domain.Interfaces.IRepositories
{
    public interface IMessageRepository : IRepository<Message>
    {
        Task<(IEnumerable<Message> Messages, int TotalCount)> GetPagedMessagesAsync(long chatId,int page, int pageSize);

        Task<int> MarkMessagesAsReadAsync(long chatId, string receiverId);
    }
}
