using SafeTrace.Application.DTOs.Message;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IChatNotifier
    {
        Task SendMessageAsync(MessageDto message);

        Task NotifyMessageDeletedForEveryone(
        long chatId,
        long messageId,
        DateTime deletedAt);

        Task NotifyMessagesReadAsync(
        long chatId,
        string userId);
        }
}
