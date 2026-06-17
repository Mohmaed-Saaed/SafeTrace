using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.Chat;
using SafeTrace.Application.Interfaces.IServices;
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
        public async Task<IActionResult> StartChat([FromBody] StartChatRequest request, string userId)
        {
            //var userId = GetCurrentUserId();
            var chat = await _chatService.StartOrGetChatAsync(request.CaseId,userId);
            return Ok(chat);
        }

        [HttpGet]
        public async Task<IActionResult> GetUserChats([FromQuery]string userId)
        {
            var chats = await _chatService.GetUserChatsAsync(userId);
            return Ok(chats);
        }

        [HttpGet("{chatId:long}")]
        public async Task<IActionResult> GetChatDetails([FromRoute] long chatId,[FromQuery] string userId)
        {
            //var userId = GetCurrentUserId();
            var chat = await _chatService.GetChatDetailsAsync(chatId, userId);
            return Ok(chat);
        }

        [HttpGet("{chatId:long}/messages")]
        public async Task<IActionResult> GetMessages ([FromQuery] string userId,
           [FromRoute] long chatId, [FromQuery] int page =1 , [FromQuery] int pageSize = 20)
        {
            if(page < 1) page = 1;
            if(pageSize <1) pageSize = 20;
            if(pageSize > 100) pageSize = 100;

            //var userId = GetCurrentUserId();
            var messages = await _chatService.GetPaginatedMessagesAsync(chatId, userId,page, pageSize);
            return Ok(messages);

        }

        //private string GetCurrentUserId()
        //{
        //    return User.FindFirstValue(ClaimTypes.NameIdentifier)
        //        ?? throw new UnauthorizedAccessException("User identity could not be resolved.");
        //}
    }
}
