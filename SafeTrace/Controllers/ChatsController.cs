using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.Chat;
using SafeTrace.Application.Constants;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Infrastructure.Authorization;
using System.Security.Claims;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatsController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatsController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("start")]
        [HasPermission(Permissions.Chat.Create)]
        public async Task<IActionResult> StartChat([FromBody] StartChatRequest request)
        {
            var userId = GetCurrentUserId();
            var chat = await _chatService.StartOrGetChatAsync(request.CaseId, userId);
            return Ok(chat);
        }

        [HttpGet]
        [HasPermission(Permissions.Chat.GetMyChats)]
        public async Task<IActionResult> GetUserChats()
        {
            var userId = GetCurrentUserId();
            var chats = await _chatService.GetUserChatsAsync(userId);
            return Ok(chats);
        }

        [HttpGet("{chatId:long}")]
        [HasPermission(Permissions.Chat.GetById)]
        public async Task<IActionResult> GetChatDetails([FromRoute] long chatId)
        {
            var userId = GetCurrentUserId();
            var chat = await _chatService.GetChatDetailsAsync(chatId, userId, IsAdmin);
            return Ok(chat);
        }

        [HttpGet("{chatId:long}/messages")]
        [HasPermission(Permissions.Chat.GetMessages)]
        public async Task<IActionResult> GetMessages(
           [FromRoute] long chatId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var userId = GetCurrentUserId();
            var messages = await _chatService.GetPaginatedMessagesAsync(chatId, userId,IsAdmin, page, pageSize);
            return Ok(messages);

        }

        [HttpDelete("{chatId:long}")]
        [HasPermission(Permissions.Chat.SoftDelete)]
        public async Task<IActionResult> DeleteChat(long chatId)
        {
            var userId = GetCurrentUserId();
            var deleted = await _chatService.DeleteChatAsync(chatId, userId);
            return Ok(deleted);
        }

        [HttpDelete("{chatId:long}/hard-delete")]
        [HasPermission(Permissions.Chat.HardDelete)]
        public async Task<IActionResult> DeleteChatByAdmin (long chatId)
        {
            var deleted = await _chatService.DeleteChatByAdminAsync(chatId);
            return Ok(deleted);
        }

        [HttpGet("admin/chats")]
        [HasPermission(Permissions.Chat.GetAll)]
        public async Task<IActionResult> GetAllChats(
            [FromQuery] ChatFilterDto filter,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        {
            var result = await _chatService.GetAllChatsAsync(page, pageSize, filter);
            return Ok(result);
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User identity could not be resolved.");
        }

        private bool IsAdmin => User.IsInRole("Admin");

    }
}


