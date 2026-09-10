using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.Message;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Infrastructure.Authorization;
using System.Security.Claims;

namespace SafeTrace.API.Controllers
{
    public class MessagesController : BaseApiController
    {
        private readonly IMessageService _messageService;

        public MessagesController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        /// <summary>
        /// Sends a new message.
        /// </summary>
        /// <remarks>
        /// Supports sending text messages with optional attachments.
        /// </remarks>
        /// <response code="201">Message sent successfully.</response>
        /// <response code="400">Invalid request.</response>
        /// <response code="404">Chat not found.</response>
        /// 
        [HttpPost("send")]
        [Consumes("multipart/form-data")]
        [Authorize]
        [EnableRateLimiting(RateLimitPolicies.ChatLimit)]
        public async Task<IActionResult> SendMessage([FromForm] SendMessageRequest request)
        {
            var senderId = CurrentUserId;
            var message = await _messageService.SendMessageAsync(request, senderId);
            return CreatedAtAction(
                actionName: null,
                value: message);
        }

        /// <summary>
        /// Marks all unread messages in a chat as read.
        /// </summary>
        /// <param name="chatId">Chat identifier.</param>
        /// <response code="200">Messages marked as read.</response>
        /// <response code="404">Chat not found.</response>
        /// 
        [HttpPut("{chatId}/read")]
        [Authorize]
        public async Task<IActionResult> MarkAsRead([FromRoute] long chatId)
        {
            var userId = CurrentUserId;
            var result = await _messageService.MarkMessagesAsReadAsync(chatId, userId);
            return Ok(result);
        }

        /// <summary>
        /// Deletes a message for the current user only.
        /// </summary>
        /// <remarks>
        /// The message remains visible to other chat participants.
        /// </remarks>
        /// <param name="messageId">Message identifier.</param>
        /// <response code="204">Message deleted.</response>
        /// <response code="404">Message not found.</response>
        /// 
        [HttpDelete("{messageId}")]
        [Authorize]
        public async Task<IActionResult> DeleteMessage([FromRoute] long messageId)
        {
            var userId = CurrentUserId;
            var deleted = await _messageService.DeleteMessageAsync(messageId, userId);

            return Ok(deleted);
        }

        /// <summary>
        /// Deletes a message for all chat participants.
        /// </summary>
        /// <remarks>
        /// Can only be performed by the message sender within the allowed deletion period (if applicable).
        /// </remarks>
        /// <param name="messageId">Message identifier.</param>
        /// <response code="204">Message deleted for everyone.</response>
        /// <response code="403">Not allowed.</response>
        /// <response code="404">Message not found.</response>
        /// 
        [HttpDelete("{messageId}/everyone")]
        [HasPermission(Permissions.Chat.DeleteMessageForEveryone)]
        public async Task<IActionResult> DeleteMessageForEveryone([FromRoute]long messageId)
        {
            var userId = CurrentUserId;
            var deleted = await _messageService.DeleteMessageForEveryoneAsync(messageId, userId);

            return Ok(deleted);
        }


    }
}
