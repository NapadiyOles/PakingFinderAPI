namespace ParkingFinder.Business.DTOs;

public class ParkingSpotInfo
{
    public ParkingSpotInfo(Guid id, decimal latitude, decimal longitude)
    {
        Id = id;
        Latitude = latitude;
        Longitude = longitude;
    }

    public Guid Id { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}