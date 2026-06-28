using Microsoft.AspNetCore.Mvc;
using SafeTrace.Application.Constants;
using SafeTrace.Application.DTOs.User.Request;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Infrastructure.Authorization;

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
        [HasPermission(Permissions.Users.GetAll)]
        public async Task<IActionResult> GetAllUsers([FromQuery] UserFilterDto filterDto)
        {
            var response = await _userService.GetAllUsersAsync(filterDto);
            return Ok(response);
        }

        [HttpGet("{userId}")]
        [HasPermission(Permissions.Users.GetById)]
        public async Task<IActionResult> GetUserById(string userId)
        {
            var response = await _userService.GetUserByIdAsync(userId);
            return Ok(response);
        }

        [HttpPost("register-by-admin")]
        [HasPermission(Permissions.Users.RegisterByAdmin)]
        public async Task<IActionResult> RegisterByAdmin([FromBody] RegisterByAdminDto dto)
        {
            var response = await _userService.RegisterByAdminAsync(dto);
            return Ok(response);
        }

        [HttpPost("ChangeRole")]
        [HasPermission(Permissions.Users.ChangeRole)]
        public async Task<IActionResult> ChangeUserRole([FromBody] ChangeUserRoleDto dto)
        {
            var response = await _userService.ChangeUserRoleAsync(dto);
            return Ok(response);
        }

        [HttpPost("approve/{userId}")]
        [HasPermission(Permissions.Users.Approve)]
        public async Task<IActionResult> ApproveUser(string userId)
        {
            var response = _userService.ApproveUserAsync(userId);
            return Ok(response);
        }

        [HttpPost("reject/{userId}")]
        [HasPermission(Permissions.Users.Reject)]
        public async Task<IActionResult> RejectUser(string userId)
        {
            var response = _userService.RejectUserAsync(userId);
            return Ok(response);
        }

        [HttpPost("toggle-block/{userId}")]
        [HasPermission(Permissions.Users.ToggleBlock)]
        public async Task<IActionResult> ToggleBlockStatus(string userId)
        {
            var response = await _userService.ToggleUserBlockStatusAsync(userId);
            return Ok(response);
        }

        [HttpGet("GetPermissions/{userId}")]
        [HasPermission(Permissions.Users.GetPermissions)]
        public async Task<IActionResult> GetUserPermissions(string userId)
        {
            var response = await _userService.GetUserPermissionsAsync(userId);
            return Ok(response);
        }

        [HttpPost("AssignPermissions")]
        [HasPermission(Permissions.Users.AssignPermissions)]
        public async Task<IActionResult> AssignUserPermissions([FromBody] AssignUserPermissionsDto dto)
        {
            var response = await _userService.AssignUserPermissionsAsync(dto);
            return Ok(response);
        }
    }
}