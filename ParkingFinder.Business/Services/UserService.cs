using Microsoft.EntityFrameworkCore;
using ParkingFinder.Business.DTOs;
using ParkingFinder.Business.Exceptions;
using ParkingFinder.Business.Interfaces;
using ParkingFinder.Data;
using ParkingFinder.Data.Entities;

namespace ParkingFinder.Business.Services;

public class UserService : IUserService
{
    private readonly Database _database;

    public UserService(Database database)
    {
        _database = database;
    }
    
    public async Task<IEnumerable<UserInfo>> GetAllAsync()
    {
         var list = await _database.Users.ToListAsync();

         return list.Select(e => new UserInfo
         {
             Guid = e.Id.ToString(),
             Name = e.Name,
             Email = e.Email,
         });
    }

    public async Task<string> GetIdByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email can't be empty");

        User? user;
        if((user = await _database.Users.FirstOrDefaultAsync(e => e.Email == email)) is null)
            throw new UnauthorizedException("User with this email is not registered");

        return user.Id.ToString();
    }

    public async Task UpdateRoleAsync(string email, string role)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email can't be empty");

        var user = await _database.Users.FirstOrDefaultAsync(e => e.Email == email);
        
        if (user is null)
            throw new UnauthorizedException("User with this email is not registered");

        if (user.Role == role.ToString())
            throw new RoleException($"The user is already {role.ToString()}");

        user.Role = role;
        
        await _database.SaveChangesAsync();
    }
    
    public async Task DeleteUserAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email can't be empty");

        var user = await _database.Users.FirstOrDefaultAsync(e => e.Email == email);
        
        if (user is null)
            throw new UnauthorizedException("User with this email is not registered");

        _database.Users.Remove(user);
        await _database.SaveChangesAsync();
    }
}