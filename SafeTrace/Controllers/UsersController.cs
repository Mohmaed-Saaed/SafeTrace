using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.DTOs.User.Request;
using SafeTrace.Application.Interfaces.IServices;

namespace SafeTrace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers([FromQuery] UserFilterDto filterDto)
        {
            var response = await _userService.GetAllUsersAsync(filterDto);
            return Ok(response);
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(string userId)
        {
            var response = await _userService.GetUserByIdAsync(userId);
            return Ok(response);
        }

        [HttpPost("{userId}/roles")]
        public async Task<IActionResult> ChangeUserRole(string userId, [FromBody] ChangeUserRoleDto dto)
        {
            var response = await _userService.ChangeUserRoleAsync(userId, dto);
            return Ok(response);
        }

        [HttpGet("{userId}/permissions")]
        public async Task<IActionResult> GetUserPermissions(string userId)
        {
            var response = await _userService.GetUserPermissionsAsync(userId);
            return Ok(response);
        }

        [HttpPost("{userId}/permissions")]
        public async Task<IActionResult> AssignUserPermissions(string userId, [FromBody] AssignUserPermissionsDto dto)
        {
            var response = await _userService.AssignUserPermissionsAsync(userId, dto);
            return Ok(response);
        }
    }
}