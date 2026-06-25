using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using SafeTrace.Infrastructure.Authorization;

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
        public async Task<ActionResult> GetAllNotifications()
        {
            var notifications = await _notificationService.GetAllrNotificationsAsync();

            if (notifications == null || !notifications.Data.Any())
            {
                return NotFound(
                    ApiResponse<IEnumerable<Notification>>
                        .Fail("No notifications found"));
            }

            return Ok(notifications);
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



        [HttpGet("my-Notifications")]
        [HasPermission(Permissions.Notifications.GetMyNotifications)]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _notificationService.GetUserNotificationsAsync(userId!);
            return Ok(notifications);
        }

        [HttpDelete("{id}")]
        [HasPermission(Permissions.Notifications.DeleteNotification)]
        public async Task<IActionResult> DeleteNotification(long id)
        {
            var result = await _notificationService.RemoveNotificationAsync(id);
            if (!result.Data) return NotFound();
            return Ok(result);
        }

        [HttpPut("{id}/MarkAsRead")]
        [HasPermission(Permissions.Notifications.MarkAsRead)]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return Ok();
        }

        [HttpPut("MarkAllAsRead")]
        [HasPermission(Permissions.Notifications.MarkAllAsRead)]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _notificationService.MarkAllAsReadAsync(userId!);
            return Ok();
        }
    }
}
