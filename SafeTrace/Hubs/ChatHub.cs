using Microsoft.AspNetCore.SignalR;
using SafeTrace.Application.Interfaces.IServices;
using System.Security.Claims;

namespace SafeTrace.API.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly IMessageService _messageService;

        public ChatHub(ILogger<ChatHub> logger,IMessageService messageService)
        {
            _logger = logger;
            _messageService = messageService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();

            //await Groups.AddToGroupAsync(Context.ConnectionId,UserGroupName(userId));
            _logger.LogInformation("User {UserId} connected to ChatHub (connectionId: {ConnectionId}).",
                userId, Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetCurrentUserId();

            //await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroupName(userId));

            if (exception != null) {
                _logger.LogWarning(exception, "User {UserId} disconnected from ChatHub with an error.", userId);
            }
            else
            {
                _logger.LogInformation("User {UserId} disconnected from ChatHub (connectionId: {ConnectionId}).",
                    userId, Context.ConnectionId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        
        public async Task JoinChat(long chatId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ChatGroupName(chatId));
            _logger.LogDebug("Connection {ConnectionId} joined chat group {ChatId}.",
                Context.ConnectionId, chatId);
        }

        public async Task LeaveChat(long chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ChatGroupName(chatId));
        }

        /// <summary>
        /// Called by the client when the user reads a chat. Persists the read-state then
        /// broadcasts it through the same notifier used for messages, so there's one place
        /// that owns "who gets told what" logic.
        /// </summary>
        public async Task MarkAsRead(long chatId)
        {
            var userId = GetCurrentUserId();

            // TODO: نادي هنا على application service يسجل حالة "مقروءة" في الـ DB، مثلاً:
             await _messageService.MarkMessagesAsReadAsync(chatId, userId);

            await Clients.Group(ChatGroupName(chatId))
                    .SendAsync("MessagesRead", new
                    {
                        chatId,
                        userId
                    });
        }

        //private string GetCurrentUserId()
        //{
        //    var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        //    if (string.IsNullOrEmpty(userId))
        //        throw new HubException("Unauthorized");

        //    return userId;
        //}

        private string GetCurrentUserId()
        {
            var userId = Context.GetHttpContext()?
                    .Request.Query["userId"]
                    .ToString();

            if (string.IsNullOrEmpty(userId))
                throw new HubException("Unauthorized");

            return userId;
        }

        private static string ChatGroupName(long chatId) => $"chat_{chatId}";


    }
}
