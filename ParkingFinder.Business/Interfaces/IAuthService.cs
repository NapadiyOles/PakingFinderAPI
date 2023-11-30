using ParkingFinder.Business.DTOs;

namespace ParkingFinder.Business.Interfaces;

public interface IAuthService
{
    Task<UserInfo> RegisterAsync(UserInfo user);
    Task<UserInfo> LogInAsync(UserInfo user);
}