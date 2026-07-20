using Microsoft.AspNetCore.SignalR;
using SafeTrace.Application.DTOs.Message;
using SafeTrace.Application.Interfaces.IServices;

namespace SafeTrace.API.Hubs
{
    public class SignalRChatNotifier : IChatNotifier
    {
        private readonly IHubContext<ChatHub> _hubContext;

        public SignalRChatNotifier (IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendMessageAsync(MessageDto message)
        {
            await _hubContext.Clients.Group($"chat_{message.ChatId}")
                .SendAsync("ReceiveMessage", message);
        }

        public async Task NotifyMessageDeletedForEveryone(
        long chatId,
        long messageId)
        {
            await _hubContext.Clients
            .Group($"chat_{chatId}")
            .SendAsync("MessageDeletedForEveryone", new
            {
                chatId,
                messageId
            });
        }
    }
}
