namespace ParkingFinder.API.Models;

public class Coordinates
{
    public Coordinates(decimal latitude, decimal longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}