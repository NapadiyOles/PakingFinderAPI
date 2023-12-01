namespace ParkingFinder.Data.Entities;

public record OccupationServiceProps
{
    public int Id { get; set; }
    public TimeSpan TotalTime { get; set; }
    public TimeSpan OnResumeTime { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime FinishTime { get; set; }
}