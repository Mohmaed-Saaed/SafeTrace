using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using SafeTrace.Domain.Interfaces.IRepositories;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]

    public class NotificationController : ControllerBase
    {
        private readonly INotificationServices _notificationService;
        public NotificationController(INotificationServices notificationService)
        {
            _notificationService = notificationService;
        }


        #region test
        [HttpGet("Test")]
        public async Task<ActionResult<ApiResponse<IEnumerable<Notification>>>> GetAllNotifications()
        {
            var notifications = await _notificationService.GetAllrNotificationsAsync();

            if (notifications == null || !notifications.Any())
            {
                return NotFound(
                    ApiResponse<IEnumerable<Notification>>
                        .Fail("No notifications found"));
            }

            return Ok(
                ApiResponse<IEnumerable<Notification>>
                    .Ok(notifications, "Notifications retrieved successfully"));
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _notificationService.SendNotificationAsync(dto);
            return Ok();
        }

        #endregion
        //test
        [HttpGet("my-Notifications")]
        public async Task<IActionResult> GetMyNotifications([FromQuery] string userId)
        {
            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            return Ok(notifications);
        }

        //[HttpGet("my-Notifications")]
        //public async Task<IActionResult> GetMyNotifications()
        //{
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    var notifications = await _notificationService.GetUserNotificationsAsync(userId!);
        //    return Ok(notifications);
        //}

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(long id)
        {
            var result = await _notificationService.RemoveNotificationAsync(id);
            if (!result) return NotFound();
            return Ok();
        }

        [HttpPut("{id}/MarkAsRead")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return Ok();
        }

        [HttpPut("MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _notificationService.MarkAllAsReadAsync(userId!);
            return Ok();
        }
    }
}
