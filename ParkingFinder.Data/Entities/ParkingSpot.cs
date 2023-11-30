namespace ParkingFinder.Data.Entities;

public record ParkingSpot
{
    private const double DegreesToRadians = Math.PI / 180.0;
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
    public double RadianLatitude => (double)Latitude * DegreesToRadians;
    public double RadianLongitude => (double)Latitude * DegreesToRadians;
    public bool Occupied { get; set; }
    public double OccupationRatio { get; set; }
}