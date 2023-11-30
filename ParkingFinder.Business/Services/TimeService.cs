using ParkingFinder.Data;

namespace ParkingFinder.Business.Services;

public class TimeService
{
    private readonly Database _database;

    public TimeService(Database database)
    {
        _database = database;
    }

    public void CalculateOccupation()
    {
        
    }
}