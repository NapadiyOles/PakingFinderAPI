using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingFinder.API.Filters;
using ParkingFinder.API.Models;
using ParkingFinder.Business.DTOs;
using ParkingFinder.Business.Interfaces;

namespace ParkingFinder.API.Controllers;

[ApiController]
[Route("authentication")]
[ExceptionFilter]
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
    public async Task<ActionResult<UserTokenModel>> Register(UserRegisterModel model)
    {
        var user = new UserInfo
        {
            Name = model.Name,
            Email = model.Email,
            Password = model.Password,
        };
        
        var result = await _auth.RegisterAsync(user);
        
        var output = new UserTokenModel(
            guid: result.Guid,
            name: result.Name,
            email: model.Email,
            token: result.Token
        );
        
        return Ok(output);
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserTokenModel>> LogIn(UserLoginModel model)
    {
        var user = new UserInfo
        {
            Email = model.Email,
            Password = model.Password,
        };
        
        var result = await _auth.LogInAsync(user);

        var output = new UserTokenModel(
            guid: result.Guid,
            name: result.Name,
            email: model.Email,
            token: result.Token
        );
        
        return Ok(output);
    }
}