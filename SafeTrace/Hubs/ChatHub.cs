using Microsoft.AspNetCore.SignalR;
using SafeTrace.Application.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SafeTrace.API.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly IChatPresenceService _chatPresenceService;

        public ChatHub(ILogger<ChatHub> logger, IChatPresenceService chatPresenceService)
        {
            _logger = logger;
            _chatPresenceService = chatPresenceService;
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
            var userId = GetCurrentUserId();
            _chatPresenceService.Join(userId, chatId);
            await Groups.AddToGroupAsync(Context.ConnectionId, ChatGroupName(chatId));
            _logger.LogDebug("Connection {ConnectionId} joined chat group {ChatId}.",
                Context.ConnectionId, chatId);
        }

        public async Task LeaveChat(long chatId)
        {
            var userId = GetCurrentUserId();
            _chatPresenceService.Leave(userId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ChatGroupName(chatId));
        }


        private string GetCurrentUserId()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                throw new HubException("Unauthorized");

            return userId;
        }

        //private string GetCurrentUserId()
        //{
        //    var userId = Context.GetHttpContext()?
        //            .Request.Query["userId"]
        //            .ToString();

        //    if (string.IsNullOrEmpty(userId))
        //        throw new HubException("Unauthorized");

        //    return userId;
        //}

        private static string ChatGroupName(long chatId) => $"chat_{chatId}";


    }
}
