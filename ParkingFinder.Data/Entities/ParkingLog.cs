namespace ParkingFinder.Data.Entities;

public enum ParkingStatus
{
    Entering,
    Leaving,
    Booking,
    Unavailable
}

public record ParkingLog
{
    public ParkingLog(Guid userId, Guid spotId)
    {
        SpotId = spotId;
        UserId = userId;
    }

    public int Id { get; set; }
    public Guid SpotId { get; set; }
    public ParkingSpot Spot { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    public DateTime EnterTime { get; set; }
    public DateTime LeaveTime { get; set; }
    public ParkingStatus Status { get; set; }
}