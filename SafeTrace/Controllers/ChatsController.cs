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
        /// Retrieves the information required to display the Start Chat page.
        /// </summary>
        /// <remarks>
        /// Returns the case summary, participant information, and whether a chat
        /// already exists between the current user and the case owner.
        /// This endpoint does not create a chat.
        /// </remarks>
        /// <param name="caseId">The ID of the case.</param>
        /// <response code="200">Start chat context retrieved successfully.</response>
        /// <response code="400">The current user cannot start a chat for their own case.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="404">The specified case was not found.</response>
        [HttpGet("start-context/{caseId}")]
        [HasPermission(Permissions.Chat.StartContext)]
        public async Task<IActionResult> GetStartContext(long caseId)
        {
            var userId = GetCurrentUserId();
            var result = await _chatService.GetStartChatContextAsync(caseId, userId);
            return Ok(result);  
        }

        /// <summary>
        /// Creates a new chat or returns the existing one.
        /// </summary>
        /// <remarks>
        /// Starts a conversation between the current user and the owner of the specified case.
        /// If a chat already exists between both participants for the same case,
        /// the existing chat is returned instead of creating a new one.
        /// </remarks>
        /// <param name="request">Contains the case identifier.</param>
        /// <response code="200">Chat created successfully or an existing chat was returned.</response>
        /// <response code="400">Invalid request or the user attempted to start a chat on their own case.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">The user does not have permission to create chats.</response>
        /// <response code="404">The specified case was not found.</response>
        [HttpPost("create")]
        [HasPermission(Permissions.Chat.Create)]
        public async Task<IActionResult> CreateChat([FromBody] StartChatRequest request)
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
           [FromRoute] long chatId)
        {
            var userId = GetCurrentUserId();
            var messages = await _chatService.GetPaginatedMessagesAsync(chatId, userId,IsAdmin);
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


