namespace ParkingFinder.Data.Entities;

public record ParkingTime
{
    public ParkingTime(Guid spotId, TimeSpan duration, DateTime recordTime)
    {
        SpotId = spotId;
        Duration = duration;
        RecordTime = recordTime;
    }

    public int Id { get; set; }
    public Guid SpotId { get; set; }
    public ParkingSpot Spot { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime RecordTime { get; set; }
}