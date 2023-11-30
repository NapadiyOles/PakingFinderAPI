using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ParkingFinder.Business.DTOs;
using ParkingFinder.Business.Exceptions;
using ParkingFinder.Business.Interfaces;
using ParkingFinder.Business.Utils;
using ParkingFinder.Data;
using ParkingFinder.Data.Entities;

namespace ParkingFinder.Business.Services;

public class AuthService : IAuthService
{
    private readonly Database _database;
    private readonly IConfiguration _config;

    public AuthService(Database database, IConfiguration config)
    {
        _database = database;
        _config = config;
        
        CheckAndAddDefaultAdmin();
    }

    public async Task<UserInfo> RegisterAsync(UserInfo info)
    {
        if (info is null) throw new ArgumentNullException(nameof(info));

        if (string.IsNullOrWhiteSpace(info.Name))
            throw new ArgumentException("Name can't be empty");

        if (string.IsNullOrWhiteSpace(info.Email))
            throw new ArgumentException("Email can't be empty");

        if (await _database.Users.AnyAsync(e => e.Email == info.Email))
            throw new AuthException("User with this email already exists");

        if (string.IsNullOrWhiteSpace(info.Password))
            throw new ArgumentException("Password can't be empty");
        
        if(info.Password.Length < 5)
            throw new ArgumentException("Password can't be less then 5 characters long");

        var entity = new User(
            name: info.Name,
            email: info.Email,
            role: Role.User,
            password: BCrypt.Net.BCrypt.HashPassword(info.Password)
        );
        
        await _database.Users.AddAsync(entity);
        await _database.SaveChangesAsync();
        
        var token = WriteToken(entity);

        var result = new UserInfo
        {
            Guid = entity.Id.ToString(),
            Name = entity.Name,
            Email = entity.Email,
            Token = token,
        };

        return result;
    }

    public async Task<UserInfo> LogInAsync(UserInfo info)
    {
        if (info is null) throw new ArgumentNullException(nameof(info));
        
        if (string.IsNullOrWhiteSpace(info.Email))
            throw new ArgumentException("Email can't be empty");

        var entity = await _database.Users.FirstOrDefaultAsync(e => e.Email == info.Email);
        
        if (entity is null)
            throw new UnauthorizedException("User with this email is not registered");
        
        if (string.IsNullOrWhiteSpace(info.Password))
            throw new ArgumentException("Password can't be empty");
        
        if(info.Password.Length < 5)
            throw new ArgumentException("Password can't be less then 5 characters long");


        if (!BCrypt.Net.BCrypt.Verify(info.Password, entity.Password))
            throw new AuthException("Password is not correct");
        
        var token = entity.Role == Role.Admin ? 
            WriteToken(entity, Role.Admin, Role.User) : 
            WriteToken(entity, Role.User);
        
        var result = new UserInfo
        {
            Guid = entity.Id.ToString(),
            Name = entity.Name,
            Email = entity.Email,
            Token = token,
        };
        
        return result;
    }

    private string WriteToken(User user, params string[] roles)
    {
        var authClaims = new List<Claim>
        {
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        var claims = roles.Select(e => new Claim(ClaimTypes.Role, e));
        authClaims.AddRange(claims);

        var tokenHandler = new JwtSecurityTokenHandler();
        var authSignInKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Secret"]!));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(authClaims),
            SigningCredentials = new SigningCredentials(
                authSignInKey,
                SecurityAlgorithms.HmacSha256Signature
            ),
            Expires = DateTime.UtcNow.AddMonths(1)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private void CheckAndAddDefaultAdmin()
    {
        if (!_database.Users.Any(e => e.Role == Role.Admin))
        {
            var user = new User(
                name: "admin",
                email: "admin@email.com",
                role: Role.Admin,
                password: BCrypt.Net.BCrypt.HashPassword("admin")
            );
            
            _database.Users.Add(user);
            _database.SaveChanges();
        }
    }
}