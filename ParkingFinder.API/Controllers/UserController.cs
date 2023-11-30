using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingFinder.API.Filters;
using ParkingFinder.API.Models;
using ParkingFinder.Business.DTOs;
using ParkingFinder.Business.Interfaces;
using ParkingFinder.Business.Utils;

namespace ParkingFinder.API.Controllers;

[ApiController]
[Route("users")]
[ExceptionFilter]
[Authorize]
public class UserController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IUserService _service;

    public UserController(ILogger<AuthController> logger, IUserService service)
    {
        _logger = logger;
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<IEnumerable<UserOutputModel>>> GetAllUsers()
    {
        var list = await _service.GetAllAsync();

        var users = list.Select(e =>
            new UserOutputModel(
                guid: e.Guid.ToString(),
                name: e.Name,
                email: e.Email
            ));
        
        return Ok(users);
    }

    [HttpGet("get_id")]
    [Authorize(Roles = Role.User)]
    public async Task<ActionResult<string>> GetUserId(string email)
    {
        var id = await _service.GetIdByEmail(email);
        return Ok(id);
    }

    [HttpPatch("get_admin")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult> GetAdminRole(string email)
    {
        await _service.UpdateRoleAsync(email, Role.Admin);
        return Ok();
    }
    
    [HttpPatch("get_user")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult> GetUserRole(string email)
    {
        await _service.UpdateRoleAsync(email, Role.User);
        return Ok();
    }

    [HttpDelete("delete")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult> DeleteUser(string email)
    {
        await _service.DeleteUserAsync(email);
        return Ok();
    }
}