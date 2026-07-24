using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using SafeTrace.Infrastructure.Authorization;
using System.Security.Claims;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class NotificationController : ControllerBase
    {
        private readonly INotificationServices _notificationService;
        public NotificationController(INotificationServices notificationService)
        {
            _notificationService = notificationService;
        }


        #region test
        [HttpPut("SendNotify")]
        public async Task<IActionResult> SendNotify([FromBody] SendNotificationDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _notificationService.SendNotificationAsync(dto);

            return Ok();
        }
        #endregion

        /// <summary>
        /// للحصول علي اشعاراتي
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet("my-Notifications")]
        [Authorize]
        public async Task<IActionResult> GetMyNotifications(
            [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _notificationService.GetUserNotificationsAsync(userId!, page,
        pageSize);
            return Ok(notifications);
        }

        /// <summary>
        /// لحذف اشعار معين 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteNotification(long id)
        {
            var result = await _notificationService.RemoveNotificationAsync(id);
            if (!result.Data) return NotFound();
            return Ok(result);
        }
        /// <summary>
        /// تحديد اشعار معين كمقروء
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPut("{id}/MarkAsRead")]
        [Authorize]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return Ok();
        }
        /// <summary>
        /// تحديد كل الاشعارات كمقروء 
        /// </summary>
        /// <returns></returns>
        [HttpPut("MarkAllAsRead")]
        [Authorize]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _notificationService.MarkAllAsReadAsync(userId!);
            return Ok();
        }
    }
}
