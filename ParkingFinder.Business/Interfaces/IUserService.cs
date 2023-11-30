using ParkingFinder.Business.DTOs;
using ParkingFinder.Business.Utils;

namespace ParkingFinder.Business.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserInfo>> GetAllAsync();
    Task<string> GetIdByEmail(string email);
    Task UpdateRoleAsync(string email, string role);
    Task DeleteUserAsync(string email);
}