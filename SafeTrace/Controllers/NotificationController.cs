using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> GetAllNotifications()
        {
            var notifies = await _notificationService.GetAllrNotificationsAsync();
            if (notifies == null) return NotFound();
            return Ok(notifies);
        }

        #endregion

        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _notificationService.GetUserNotificationsAsync(userId!);
            return Ok(notifications);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _notificationService.RemoveNotificationAsync(id);
            if (!result) return NotFound();
            return Ok();
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return Ok();
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _notificationService.MarkAllAsReadAsync(userId!);
            return Ok();
        }
    }
}
