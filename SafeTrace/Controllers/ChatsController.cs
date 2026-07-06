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

        /// <summary>
        /// Starts a new chat between two users.
        /// </summary>
        /// <remarks>
        /// Creates a chat if one does not already exist between the participants.
        /// Returns the existing chat otherwise.
        /// </remarks>
        /// <response code="200">Chat created or already exists.</response>
        /// <response code="400">Invalid request.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">User does not have permission.</response>
        [HttpPost("start")]
        [HasPermission(Permissions.Chat.Create)]
        public async Task<IActionResult> StartChat([FromBody] StartChatRequest request)
        {
            var userId = GetCurrentUserId();
            var chat = await _chatService.StartOrGetChatAsync(request.CaseId, userId);
            return Ok(chat);
        }

        /// <summary>
        /// Retrieves all chats for the current user.
        /// </summary>
        /// <remarks>
        /// Returns chats ordered by the latest message.
        /// </remarks>
        /// <response code="200">Chats retrieved successfully.</response>
        /// <response code="401">Unauthorized.</response>

        [HttpGet]
        [HasPermission(Permissions.Chat.GetMyChats)]
        public async Task<IActionResult> GetUserChats()
        {
            var userId = GetCurrentUserId();
            var chats = await _chatService.GetUserChatsAsync(userId);
            return Ok(chats);
        }

        /// <summary>
        /// Retrieves detailed information about a specific chat.
        /// </summary>
        /// <remarks>
        /// Returns chat metadata including:
        /// - Chat identifier.
        /// - Related case information.
        /// - Sender and receiver details.
        /// - The other participant's name for the current user.
        /// - Chat creation date.
        /// - Soft delete information (available only for administrators).
        /// </remarks>
        /// <param name="chatId">The unique identifier of the chat.</param>
        /// <response code="200">Chat details retrieved successfully.</response>
        /// <response code="401">User is not authenticated.</response>
        /// <response code="403">User is not authorized to access this chat.</response>
        /// <response code="404">Chat was not found.</response>
        [ProducesResponseType(typeof(ChatDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]

        [HttpGet("{chatId:long}")]
        [HasPermission(Permissions.Chat.GetById)]
        public async Task<IActionResult> GetChatDetails([FromRoute] long chatId)
        {
            var userId = GetCurrentUserId();
            var chat = await _chatService.GetChatDetailsAsync(chatId, userId, IsAdmin);
            return Ok(chat);
        }


        /// <summary>
        /// Retrieves messages for a chat.
        /// </summary>
        /// <param name="chatId">Chat identifier.</param>
        /// <param name="page">Page number.</param>
        /// <param name="pageSize">Number of messages per page.</param>
        /// <remarks>
        /// Messages are returned ordered from newest to oldest.
        /// </remarks>
        /// <response code="200">Messages retrieved successfully.</response>
        /// <response code="404">Chat not found.</response>
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

        /// <summary>
        /// Deletes the chat for the current user only.
        /// </summary>
        /// <remarks>
        /// The other participant can still access the chat.
        /// </remarks>
        /// <param name="chatId">Chat identifier.</param>
        /// <response code="204">Chat deleted successfully.</response>
        /// <response code="404">Chat not found.</response>
        /// 
        [HttpDelete("{chatId:long}")]
        [HasPermission(Permissions.Chat.SoftDelete)]
        public async Task<IActionResult> DeleteChat(long chatId)
        {
            var userId = GetCurrentUserId();
            var deleted = await _chatService.DeleteChatAsync(chatId, userId);
            return Ok(deleted);
        }

        /// <summary>
        /// Permanently deletes a chat.
        /// </summary>
        /// <remarks>
        /// Removes the chat and all associated messages from the system.
        /// Admin only.
        /// </remarks>
        /// <param name="chatId">Chat identifier.</param>
        /// <response code="204">Chat permanently deleted.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Chat not found.</response>

        [HttpDelete("{chatId:long}/hard-delete")]
        [HasPermission(Permissions.Chat.HardDelete)]
        public async Task<IActionResult> DeleteChatByAdmin (long chatId)
        {
            var deleted = await _chatService.DeleteChatByAdminAsync(chatId);
            return Ok(deleted);
        }

        /// <summary>
        /// Retrieves all chats in the system.
        /// </summary>
        /// <remarks>
        /// Available only for administrators.
        /// </remarks>
        /// <response code="200">Chats retrieved successfully.</response>
        /// <response code="403">Forbidden.</response>

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


