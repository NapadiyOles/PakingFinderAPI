namespace ParkingFinder.API.Models;

public class UserCoordinates
{
    public UserCoordinates(string userId, decimal latitude, decimal longitude)
    {
        UserId = userId;
        Latitude = latitude;
        Longitude = longitude;
    }

    public string UserId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}