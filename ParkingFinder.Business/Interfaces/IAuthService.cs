using ParkingFinder.Business.DTOs;

namespace ParkingFinder.Business.Interfaces;

public interface IAuthService
{
    Task RegisterAsync(UserDTO user);
    Task<string> LogInAsync(UserDTO user);
}