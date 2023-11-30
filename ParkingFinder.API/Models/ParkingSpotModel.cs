namespace ParkingFinder.API.Models;

public class ParkingSpotModel
{
    public ParkingSpotModel(Guid id, decimal latitude, decimal longitude)
    {
        Id = id;
        Latitude = latitude;
        Longitude = longitude;
    }

    public Guid Id { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

}