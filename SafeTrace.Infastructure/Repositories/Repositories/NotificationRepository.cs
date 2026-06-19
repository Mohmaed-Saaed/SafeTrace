using Microsoft.Extensions.Logging;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Interfaces.IRepositories;
using SafeTrace.Infrastructure.DataAccess;
using SafeTrace.Infrastructure.Repositories.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Infrastructure.Repositories.Repositories
{
    public class NotificationRepository : Repository<Notification>, INotificationRepository
    {
        private readonly ApplicationDbContext _context;
        public NotificationRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;

        }

        #region test
        public async Task<IEnumerable<Notification>> GetAllrNotificationsAsync()
        {
            return await _context.Notifications.OrderByDescending(n => n.CreatedAt).ToListAsync();
        }

        #endregion 
        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId)
        {
            return await _context.Notifications
                       .Where(n => n.UserId == userId)
                       .OrderByDescending(n => n.CreatedAt)
                       .ToListAsync();
        }
        public async Task<Notification?> GetNotificationByIdAsync(long id)
        {
            return await _context.Notifications.FindAsync(id);
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        }


        public async Task MarkAllAsReadAsync(string userId)
        {
            await _context.Notifications
                        .Where(n => n.UserId == userId && !n.IsRead)
                        .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        }

        public async Task MarkAsReadAsync(long id)
        {
            var notification = await GetNotificationByIdAsync(id);
            if (notification is null) return;

            notification.IsRead = true;
            _context.Notifications.Update(notification);
        }

        public async Task<bool> RemoveNotificationAsync(long id)
        {
            var notification = await GetNotificationByIdAsync(id);
            if (notification is null) return false;

            _context.Notifications.Remove(notification);
            return true;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        #region
        //public Task GetAllNotificationAsync()
        //{
        //    try
        //    {
        //        var AllNotifications = _context.Notifications.ToList();
        //        return Task.FromResult(AllNotifications);
        //    }
        //    catch
        //    {
        //        return Task.FromResult(0);
        //    }
        //}

        //public Task GetNotificationById(string id)
        //{
        //    try
        //    {
        //        var Notification = _context.Notifications.Find(id);
        //        return Task.FromResult(Notification);
        //    }
        //    catch
        //    {
        //        return Task.FromResult(0);
        //    }

        //}

        //public Task GetNotificationDetails(string notfiId)
        //{
        //    var notf = GetNotificationById(notfiId);
        //    return Task.FromResult(notf);

        //}

        ////public Task PushNotification(string userId)
        ////{
        ////    throw new NotImplementedException();
        ////}

        //public Task RemoveNotification(string notfiId)
        //{
        //    var renoved = GetNotificationById(notfiId);
        //    _context.Remove(renoved);
        //    return Task.CompletedTask;
        //}

        //public void Save()
        //{
        //    _context.SaveChanges();
        //}
        #endregion
    }
}
