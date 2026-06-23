using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.Message;
using SafeTrace.Application.Interfaces.IServices;
using System.Security.Claims;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessagesController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        [HttpPost("send")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SendMessage([FromForm] SendMessageRequest request, string senderId)
        {
            //var senderId = GetCurrentUserId();
            var message = await _messageService.SendMessageAsync(request, senderId);
            return CreatedAtAction(
                actionName: null,
                value: message);
        }

        [HttpPut("{chatId}/read")]
        public async Task<IActionResult> MarkAsRead([FromRoute] long chatId, [FromQuery] string userId)
        {
            //var userId = GetCurrentUserId();
            var result = await _messageService.MarkMessagesAsReadAsync(chatId, userId);
            return Ok(result);
        }

        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage([FromRoute] long messageId,[FromQuery] string userId)
        {
            var deleted = await _messageService.DeleteMessageAsync(messageId, userId);

            return Ok(deleted);
        }

        [HttpDelete("{messageId}/everyone")]
        public async Task<IActionResult> DeleteMessageForEveryone([FromRoute]long messageId, [FromQuery] string userId)
        {
            var deleted = await _messageService.DeleteMessageForEveryoneAsync(messageId, userId);

            return Ok(deleted);
        }


        //private string GetCurrentUserId()
        //{
        //    return User.FindFirstValue(ClaimTypes.NameIdentifier)
        //        ?? throw new UnauthorizedAccessException("User identity could not be resolved.");
        //}
    }
}
