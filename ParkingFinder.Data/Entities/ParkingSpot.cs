namespace ParkingFinder.Data.Entities;

public class ParkingSpot
{
    public ParkingSpot(decimal latitude, decimal longitude)
    {
        Id = Guid.NewGuid();
        Latitude = latitude;
        Longitude = longitude;
        OccupationRatio = 0.5;
    }

    public Guid Id { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public bool Occupied { get; set; }
    public double OccupationRatio { get; set; }
}