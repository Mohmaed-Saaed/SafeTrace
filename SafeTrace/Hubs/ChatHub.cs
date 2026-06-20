using Microsoft.AspNetCore.SignalR;

namespace SafeTrace.API.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();

            await Groups.AddToGroupAsync(Context.ConnectionId,userId);
            _logger.LogInformation("User {UserId} connected to ChatHub (connectionId: {ConnectionId}).",
                userId, Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);

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

        private string GetUserId()
        {
            var userId = Context.GetHttpContext()?
                .Request.Query["userId"]
                .ToString();

            if (string.IsNullOrEmpty(userId))
                throw new HubException("UserId is required.");

            return userId;
        }

        private static string ChatGroupName(long chatId) => $"chat_{chatId}";


    }
}
