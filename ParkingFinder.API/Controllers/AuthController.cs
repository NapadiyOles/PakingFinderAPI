using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingFinder.API.Filters;
using ParkingFinder.API.Models;
using ParkingFinder.Business.DTOs;
using ParkingFinder.Business.Interfaces;

namespace ParkingFinder.API.Controllers;

[ApiController]
[Route("authentication")]
[AuthExceptionFilters]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IAuthService _auth;

    public AuthController(ILogger<AuthController> logger, IAuthService auth)
    {
        _logger = logger;
        _auth = auth;
    }
    
    [HttpPost("register")]
    public async Task<ActionResult> Register([FromForm] UserRegisterModel model)
    {
        var user = new UserDTO(
            name: model.Name,
            email: model.Email,
            password: model.Password
        );
        
        await _auth.RegisterAsync(user);
        return Ok();
    }

    [HttpPost("login")]
    public async Task<ActionResult<string>> LogIn([FromForm] UserLoginModel model)
    {
        var user = new UserDTO(
            name: default!,
            email: model.Email,
            password: model.Password
        );
        
        var token = await _auth.LogInAsync(user);
        return Ok(token);
    }
}