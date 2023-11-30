namespace ParkingFinder.Business.DTOs;

public record UserInfo
{
    public string Guid { get; init; }
    public string Name { get; init; }
    public string Email { get; init; }
    public string Token { get; init; }
    public string Password { get; init; }
}