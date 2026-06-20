namespace SafeTrace.Domain.Interfaces.IRepositories
{
    public interface INotificationRepository : IRepository<Notification>
    {
        //test
        Task<IEnumerable<Notification>> GetAllrNotificationsAsync();
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId);
        Task<Notification?> GetNotificationByIdAsync(long id);
        Task<bool> RemoveNotificationAsync(long id);
        Task MarkAsReadAsync(long id);
        Task MarkAllAsReadAsync(string userId);
        Task<int> GetUnreadCountAsync(string userId);
        //Task SaveAsync();
    }
}
