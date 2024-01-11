namespace ParkingFinder.API.Models;

public class Result
{
    public Result(string status)
    {
        Status = status;
    }

    public string Status { get; set; }
}