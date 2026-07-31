using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Exceptions;
using System.Security.Claims;

namespace SafeTrace.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        /// <summary>
        /// يُرجع معرف المستخدم الحالي المسجل الدخول، أو يلقي استثناء UnauthorizedException إذا تعذر التحقق.
        /// </summary>
        protected string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedException("تعذر التحقق من هوية المستخدم.");

        /// <summary>
        /// يُرجع معرف المستخدم الحالي أو null إذا كان زائر غير مسجل الدخول.
        /// </summary>
        protected string? CurrentUserIdOrNull =>
            User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
