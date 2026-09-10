using SafeTrace.Application.Interfaces.IServices;
using System.Collections.Concurrent;
namespace SafeTrace.API.Hubs
{
    public class ChatPresenceService : IChatPresenceService
    {
        private readonly ConcurrentDictionary<string, long> _activeChats = new();
        private readonly ConcurrentDictionary<string, long> _emailSentChats = new();

        public void Join(string userId, long chatId)
        {
            _activeChats[userId] = chatId;
            _emailSentChats.TryRemove(userId, out _);
        }

        public void Leave(string userId)
        {
            _activeChats.TryRemove(userId, out _);
        }

        public bool IsUserInChat(string userId, long chatId)
        {
            return _activeChats.TryGetValue(userId, out var activeChatId)
                   && activeChatId == chatId;
        }

        public bool ShouldSendEmail(string userId, long chatId)
        {
            return !_emailSentChats.TryGetValue(userId, out var sentChatId)
                   || sentChatId != chatId;
        }

        public void MarkEmailAsSent(string userId, long chatId)
        {
            _emailSentChats[userId] = chatId;
        }
        public void ResetEmail(string userId)
        {
            _emailSentChats.TryRemove(userId, out _);
        }
    }
}
