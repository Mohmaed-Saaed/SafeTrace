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

        public async Task SendMessageAsync(string receiverId, MessageDto message)
        {
            await _hubContext.Clients.Group(receiverId)
                .SendAsync("ReceiveMessage", message);
        }
    }
}
