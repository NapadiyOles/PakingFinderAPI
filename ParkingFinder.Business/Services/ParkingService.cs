using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ParkingFinder.Business.DTOs;
using ParkingFinder.Business.Exceptions;
using ParkingFinder.Business.Interfaces;
using ParkingFinder.Business.Utils;
using ParkingFinder.Data;
using ParkingFinder.Data.Entities;

namespace ParkingFinder.Business.Services;

public enum Mode
{
    Realtime,
    Simulation
}

public class ParkingService : IParkingService
{
    private readonly Database _database;
    private readonly ILogger<ParkingService> _logger;
    private readonly Mode _mode;

    public ParkingService(Database database, ILogger<ParkingService> logger, Mode mode = Mode.Realtime)
    {
        _database = database;
        _logger = logger;
        _mode = mode;
    }

    public async Task<IEnumerable<ParkingSpotInfo>> GetAllParkingSpots()
    {
        var list = await _database.ParkingSpots.ToListAsync();

        return list.Select(e => new ParkingSpotInfo(
            id: e.Id,
            latitude: e.Latitude,
            longitude: e.Longitude
        ));
    }

    /// <summary>
    /// Adds new parking spot to the database
    /// </summary>
    /// <param name="info">Parking spot to add</param>
    /// <exception cref="ArgumentNullException">On parking spot is null</exception>
    public async Task AddParkingSpot(ParkingSpotInfo info)
    {
        if (info is null) throw new ArgumentNullException(nameof(info));

        var spot = new ParkingSpot(
            latitude: info.Latitude,
            longitude: info.Longitude
        );
        
        await _database.ParkingSpots.AddAsync(spot);
        await _database.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes parking spot from the database
    /// </summary>
    /// <param name="id">Id of the parking spot to delete</param>
    /// <exception cref="ArgumentException">On invalid parking spot id</exception>
    /// <exception cref="NotFoundException">On parking spot not found by id</exception>
    public async Task DeleteParkingSpot(string id)
    {
        if (!Guid.TryParse(id, out var guid)) throw new ArgumentException("Parking spot Id is not valid");
        
        var spot = await _database.ParkingSpots
            .FirstOrDefaultAsync(e => e.Id == guid);

        if (spot is null) 
            throw new NotFoundException("Parking spot with this guid was not found");

        _database.ParkingSpots.Remove(spot);
        await _database.SaveChangesAsync();
    }

    /// <summary>
    /// Gives the best parking spot relative to the user
    /// </summary>
    /// <param name="cords">Coordinates of the user</param>
    /// <param name="userId">Id of the parked user</param>
    /// <returns>Information about the calculated parking spot</returns>
    /// <exception cref="NotFoundException">On all parking spots occupied</exception>
    public async Task<ParkingSpotInfo> SuggestParkingSpot((decimal latitude, decimal longitude) cords, string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid)) throw new ArgumentException("User Id is not valid");
        
        User? user;
        if ((user = await _database.Users.FindAsync(userGuid)) is null)
            throw new NotFoundException("User with current Id was not found");
        
        const double radius = 6371008.8;
        const double convertD2R = Math.PI / 180.0;
        var query = _database.ParkingSpots
            .Where(e => !e.Occupied)
            .OrderBy(e =>
                e.OccupationRatio * Math.Sqrt(
                    Math.Pow(
                        (double)(cords.latitude - e.Latitude) * convertD2R * radius
                        , 2)
                    +
                    Math.Pow(
                        (double)(cords.longitude - e.Latitude) * convertD2R * radius *
                        Math.Cos((double)(cords.latitude + e.Latitude) * convertD2R / 2.0)
                        , 2))
            );
        
        // const double radius = 6371008.8;
        //
        // var latDiff = lat2 - lat1;
        // var lonDiff = lon2 - lon1;
        //
        // var latAvg = lat2 + lat1 / 2;
        //
        // var latDist = (double)latDiff * radius;
        // var lonDist = (double)lonDiff * radius * Math.Cos((double)latAvg);
        //
        // double distance = Math.Sqrt(latDist * latDist + lonDist * lonDist);
        
        _logger.LogDebug($"Suggest parking spot query:\n{query.ToQueryString()}");
            
        var spot = await query.FirstOrDefaultAsync();

        if (spot is null) throw new NotFoundException("All parking spots are occupied");

        spot.Occupied = true;

        await RecordLogAsync(user.Id, spot.Id, ParkingStatus.Booking);

        var result = new ParkingSpotInfo(
            id: spot.Id,
            latitude: spot.Latitude,
            longitude: spot.Longitude
        );
        
        return result;
    }

    /// <summary>
    /// Records the log for the successful user parking
    /// </summary>
    /// <param name="userId">Id of the parked user</param>
    /// <param name="spotId">Id of the parking spot</param>
    /// <exception cref="OccupationException">On non-booked parking spot</exception>
    /// <exception cref="ArgumentException">On invalid parking spot or user id</exception>
    /// <exception cref="NotFoundException">On parking spot or user not found by id</exception>
    public async Task ReportParking(string userId, string spotId)
    {
        var (spot, user) = await ValidateParkingAsync(userId, spotId);

        if (!spot.Occupied) throw new OccupationException("The parking spot booking was not successful");

        await RecordLogAsync(user.Id, spot.Id, ParkingStatus.Entering);
    }
    
    /// <summary>
    /// Records the log for the successful parking spot leaving 
    /// </summary>
    /// <param name="userId">Id of the parked user</param>
    /// <param name="spotId">Id of the parking spot</param>
    /// <exception cref="OccupationException">On parking spot not occupied</exception>
    /// <exception cref="NotFoundException">On parking spot not been registered for parking</exception>
    /// <exception cref="ArgumentException">On invalid parking spot or user id</exception>
    /// <exception cref="NotFoundException">On parking spot or user not found by id</exception>
    public async Task ReportLeaving(string userId, string spotId)
    {
        var (spot, user) = await ValidateParkingAsync(userId, spotId);
        
        if (!spot.Occupied) throw new OccupationException("This spot is currently not occupied");

        var latest = _database.ParkingLogs
            .Where(e => e.SpotId == spot.Id)
            .OrderByDescending(e => e.ReportTime)
            .FirstOrDefault();

        if (latest is null) throw new NotFoundException("This spot has never been used for parking yet");

        if (latest.UserId != user.Id)
            _logger.Log(
                LogLevel.Critical,
                $"The misallocation of the users occurred. Requested entering: {latest.UserId}, leaving {user.Id}"
            );

        spot.Occupied = false;

        await RecordLogAsync(user.Id, spot.Id, ParkingStatus.Leaving);
    }

    /// <summary>
    /// Records the log for the parking spot been marked as free but been occupied
    /// </summary>
    /// <param name="userId">Id of the parked user</param>
    /// <param name="spotId">Id of the parking spot</param>
    /// <exception cref="OccupationException">On parking spot already been marked as occupied</exception>
    /// <exception cref="ArgumentException">On invalid parking spot or user id</exception>
    /// <exception cref="NotFoundException">On parking spot or user not found by id</exception>
    public async Task ReportBlocking(string userId, string spotId)
    {
        var (spot, user) = await ValidateParkingAsync(userId, spotId);
        
        if (spot.Occupied) throw new OccupationException("This spot is already stated as occupied");
        
        spot.Occupied = true;

        await RecordLogAsync(user.Id, spot.Id, ParkingStatus.Unavailable);
    }

    /// <summary>
    /// Checks if the specific parking spot is occupied
    /// </summary>
    /// <param name="userId">Id of the parked user</param>
    /// <param name="spotId">Id of the parking spot</param>
    /// <returns>Information about the free parking spot</returns>
    /// <exception cref="ArgumentException">On invalid parking spot id</exception>
    /// <exception cref="NotFoundException">On requested parking spot not found</exception>
    /// <exception cref="OccupationException">On requested parking spot been occupied</exception>
    public async Task<ParkingSpotInfo> CheckFavouriteSpot(string userId, string spotId)
    {
        var (spot, user) = await ValidateParkingAsync(userId, spotId);

        if (spot.Occupied) throw new OccupationException("Requested parking spot is currently occupied");

        spot.Occupied = true;

        await RecordLogAsync(user.Id, spot.Id, ParkingStatus.Booking);
        
        var result = new ParkingSpotInfo(
            id: spot.Id,
            latitude: spot.Latitude,
            longitude: spot.Longitude
        );
        
        return result;
    }
    
    private async Task<(ParkingSpot spot, User user)> ValidateParkingAsync(string userId, string spotId)
    {
        if (!Guid.TryParse(userId, out var userGuid)) throw new ArgumentException("User Id is not valid");

        if (!Guid.TryParse(spotId, out var spotGuid)) throw new ArgumentException("Parking spot Id is not valid");

        User? user;
        if ((user = await _database.Users.FindAsync(userGuid)) is null)
            throw new NotFoundException("User with current Id was not found");

        ParkingSpot? spot;
        if ((spot = await _database.ParkingSpots.FindAsync(spotGuid)) is null)
            throw new NotFoundException("Parking spot with current Id was not found");
        
        return (spot, user);
    }
    
    private async Task RecordLogAsync(Guid user, Guid spot, ParkingStatus status)
    {
        var log = new ParkingLog(
            userId: user,
            spotId: spot,
            reportTime: _mode switch
            {
                Mode.Realtime => DateTime.Now,
                Mode.Simulation => Global.Time,
                _ => default
            },
            status: status
        );

        await _database.ParkingLogs.AddAsync(log);
        await _database.SaveChangesAsync();
    }
}