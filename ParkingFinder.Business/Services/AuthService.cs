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

    public async Task RegisterAsync(UserDTO dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Name can't be empty");

        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new ArgumentException("Email can't be empty");

        if (await _database.Users.AnyAsync(e => e.Email == dto.Email))
            throw new AuthException("User with this email already exists");

        if (string.IsNullOrWhiteSpace(dto.Password))
            throw new ArgumentException("Password can't be empty");
        
        if(dto.Password.Length < 5)
            throw new ArgumentException("Password can't be less then 5 characters long");

        var user = new User(
            name: dto.Name,
            email: dto.Email,
            role: Role.User,
            password: BCrypt.Net.BCrypt.HashPassword(dto.Password)
        );
        
        await _database.Users.AddAsync(user);
        await _database.SaveChangesAsync();
    }

    public async Task<string> LogInAsync(UserDTO dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        
        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new ArgumentException("Email can't be empty");

        var user = await _database.Users.FirstOrDefaultAsync(e => e.Email == dto.Email);
        
        if (user is null)
            throw new AuthException("User with this email is not registered");
        
        if (string.IsNullOrWhiteSpace(dto.Password))
            throw new ArgumentException("Password can't be empty");
        
        if(dto.Password.Length < 5)
            throw new AuthException("Password can't be less then 5 characters long");


        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
            throw new AuthException("Password is not correct");
        
        var authClaims = new List<Claim>
        {
            new (ClaimTypes.Email, user.Email),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new (ClaimTypes.Role, user.Role),
        };
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var authSignInKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Secret"]));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(authClaims),
            SigningCredentials = new SigningCredentials(
                authSignInKey,
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    private void CheckAndAddDefaultAdmin()
    {
        if (!_database.Users.Any(e => e.Role == Role.Admin))
        {
            var user = new User(
                name: "Admin",
                email: "admin@email.com",
                role: Role.Admin,
                password: BCrypt.Net.BCrypt.HashPassword("admin")
            );
            
            _database.Users.Add(user);
            _database.SaveChanges();
        }
    }
}