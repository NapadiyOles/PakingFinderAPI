namespace ParkingFinder.Data.Entities;

public enum ParkingStatus
{
    Entering,
    Exiting,
    Unavailable
}

public record ParkingLog
{
    public ParkingLog(Guid spotId, DateTime time)
    {
        ParkingSpotId = spotId;
        Time = time;
    }

    public Guid ParkingSpotId { get; set; }
    public ParkingSpot Spot { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    public DateTime Time { get; set; }
    public ParkingStatus Status { get; set; }
}