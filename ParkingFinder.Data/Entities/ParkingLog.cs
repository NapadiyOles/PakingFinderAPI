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
    public ParkingLog(Guid userId, Guid spotId, DateTime reportTime, ParkingStatus status)
    {
        SpotId = spotId;
        ReportTime = reportTime;
        Status = status;
        UserId = userId;
    }

    public int Id { get; set; }
    public Guid SpotId { get; set; }
    public ParkingSpot Spot { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    public DateTime ReportTime { get; set; }
    public ParkingStatus Status { get; set; }
}