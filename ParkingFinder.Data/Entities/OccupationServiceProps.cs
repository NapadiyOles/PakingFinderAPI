namespace ParkingFinder.Data.Entities;

public class OccupationServiceProps
{
    public int Id { get; set; }
    public long TotalTime { get; set; }
    public long OnResumeTime { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime FinishTime { get; set; }
}