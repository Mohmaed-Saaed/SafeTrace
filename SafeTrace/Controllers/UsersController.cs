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

        [HttpPost("/ChangeUserRole")]
        public async Task<IActionResult> ChangeUserRole([FromBody] ChangeUserRoleDto dto)
        {
            var response = await _userService.ChangeUserRoleAsync(dto);
            return Ok(response);
        }

        [HttpPost("/approve{userId}")]
        public async Task<IActionResult> ApproveUser(string userId)
        {

        }

        [HttpPost("/reject{userId}")]
        public async Task<IActionResult> RejectUser(string userId)
        {

        }

        [HttpGet("/GetUserPermissions")]
        public async Task<IActionResult> GetUserPermissions(string userId)
        {
            var response = await _userService.GetUserPermissionsAsync(userId);
            return Ok(response);
        }

        [HttpPost("/AssignUserPermissions")]
        public async Task<IActionResult> AssignUserPermissions([FromBody] AssignUserPermissionsDto dto)
        {
            var response = await _userService.AssignUserPermissionsAsync(dto);
            return Ok(response);
        }
    }
}