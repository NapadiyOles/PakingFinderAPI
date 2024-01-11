namespace ParkingFinder.Data.Entities;

public class ParkingTime
{
    public ParkingTime(Guid spotId, long duration, DateTime recordTime)
    {
        SpotId = spotId;
        Duration = duration;
        RecordTime = recordTime;
    }

    public int Id { get; set; }
    public Guid SpotId { get; set; }
    public ParkingSpot Spot { get; set; }
    public long Duration { get; set; }
    public DateTime RecordTime { get; set; }
}