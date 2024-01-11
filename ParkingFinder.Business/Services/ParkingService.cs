using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ParkingFinder.Business.DTOs;
using ParkingFinder.Business.Exceptions;
using ParkingFinder.Business.Interfaces;
using ParkingFinder.Data;
using ParkingFinder.Data.Entities;

namespace ParkingFinder.Business.Services;

public class ParkingService : IParkingService
{
    private readonly Database _database;
    private readonly ILogger<ParkingService> _logger;
    private readonly TimeSpan _blockDuration, _maxOccupationDuration;

    public ParkingService(Database database, ILogger<ParkingService> logger, IConfiguration cfg)
    {
        _database = database;
        _logger = logger;
        _blockDuration = TimeSpan.FromMinutes(double.Parse(cfg["Parking:BlockDuration"]!));
        _maxOccupationDuration = TimeSpan.FromDays(double.Parse(cfg["Parking:MaxOccupationDays"]!));
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

    public async Task<ParkingSpotInfo> GetParkingSpotById(string id)
    {
        if (!Guid.TryParse(id, out var guid)) throw new ArgumentException("Parking spot Id is not valid");
        
        var spot = await _database.ParkingSpots.FindAsync(guid);

        if (spot is null) 
            throw new NotFoundException("Parking spot with this guid was not found");
        
        return new ParkingSpotInfo(spot.Id, latitude: spot.Latitude, longitude: spot.Longitude);
    }
    
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
    
    public async Task<ParkingSpotInfo> SuggestParkingSpot((decimal latitude, decimal longitude) cords, string userId, DateTime now)
    {
        if (!Guid.TryParse(userId, out var userGuid)) throw new ArgumentException("User Id is not valid");
        
        User? user;
        if ((user = await _database.Users.FindAsync(userGuid)) is null)
            throw new UnauthorizedException("User with current Id was not found");

        const double radius = 6371008.8;
        const double convertD2R = Math.PI / 180.0;
        var spot = await _database.ParkingSpots
            .Where(s => !s.Occupied)
            .Select(s => new
            {
                Spot = s,
                Distance = Math.Sqrt(
                    Math.Pow((double)(cords.latitude - s.Latitude) * convertD2R * radius, 2) +
                    Math.Pow((double)(cords.longitude - s.Longitude) * convertD2R * radius *
                             Math.Cos((double)(cords.latitude + s.Latitude) * convertD2R / 2.0), 2))
            })
            .OrderBy(t => t.Spot.OccupationRatio * t.Distance)
            .ThenBy(t => t.Distance)
            .Select(t => t.Spot)
            .FirstOrDefaultAsync();
        
        if (spot is null) throw new NotFoundException("All parking spots are occupied");
        // if (spot.Occupied)
        // {
        //     return new ParkingSpotInfo(
        //         id: Guid.Empty,
        //         latitude: spot.Latitude,
        //         longitude: spot.Longitude
        //     );
        // }

        spot.Occupied = true;

        var result = new ParkingSpotInfo(
            id: spot.Id,
            latitude: spot.Latitude,
            longitude: spot.Longitude
        );
        
        var log = new ParkingLog(
            userId: user.Id,
            spotId: spot.Id
        )
        {
            Status = ParkingStatus.Booking,
            EnterTime = now
        };

        await _database.ParkingLogs.AddAsync(log);
        await _database.SaveChangesAsync();
        
        return result;
    }
    
    public async Task ReportBlocking(string userId, string spotId, DateTime now)
    {
        var (spot, user) = await ValidateParkingAsync(userId, spotId);

        if (!spot.Occupied) throw new OccupationException("The booking was not successful");

        var log = await _database.ParkingLogs
            .Where(e => e.Status == ParkingStatus.Booking && e.SpotId == spot.Id)
            .OrderByDescending(e => e.EnterTime)
            .FirstOrDefaultAsync();
        
        if(log?.UserId != user.Id) throw new OccupationException("The booking was not successful");

        log.Status = ParkingStatus.Unavailable;
        log.EnterTime = now;
        log.LeaveTime = now + _blockDuration;

        await _database.SaveChangesAsync();
    }
    
    public async Task ReportParking(string userId, string spotId, DateTime now)
    {
        var (spot, user) = await ValidateParkingAsync(userId, spotId);

        if (!spot.Occupied) throw new OccupationException("The booking was not successful");

        var log = await _database.ParkingLogs
            .Where(e => e.Status == ParkingStatus.Booking && e.SpotId == spot.Id)
            .OrderByDescending(e => e.EnterTime)
            .FirstOrDefaultAsync();
        
        if(log?.UserId != user.Id) throw new OccupationException("The booking was not successful");

        log.Status = (int)ParkingStatus.Entering;
        log.EnterTime = now;
        log.LeaveTime = now + _maxOccupationDuration;

        await _database.SaveChangesAsync();
    }
    
    public async Task ReportLeaving(string userId, string spotId, DateTime now)
    {
        var (spot, user) = await ValidateParkingAsync(userId, spotId);
        
        if (!spot.Occupied) throw new OccupationException("This spot is currently not occupied");

        var log = await _database.ParkingLogs
            .Where(e => e.Status == ParkingStatus.Entering && e.SpotId == spot.Id)
            .OrderByDescending(e => e.EnterTime)
            .FirstOrDefaultAsync();
        
        if (log is null) throw new NotFoundException("This spot is not registered for parking");

        if (log.UserId != user.Id)
            throw new OccupationException("This spot is currently registered behind a different user");

        spot.Occupied = false;
        log.Status = ParkingStatus.Leaving;
        log.LeaveTime = now;

        await _database.SaveChangesAsync();
    }
    
    public async Task<ParkingSpotInfo> CheckFavouriteSpot(string userId, string spotId, DateTime now)
    {
        var (spot, user) = await ValidateParkingAsync(userId, spotId);

        if (spot.Occupied) throw new OccupationException("Requested parking spot is currently occupied");

        spot.Occupied = true;
        
        var result = new ParkingSpotInfo(
            id: spot.Id,
            latitude: spot.Latitude,
            longitude: spot.Longitude
        );
        
        var log = new ParkingLog(
            userId: user.Id,
            spotId: spot.Id
        )
        {
            Status = ParkingStatus.Booking,
            EnterTime = now
        };

        await _database.ParkingLogs.AddAsync(log);
        await _database.SaveChangesAsync();
        
        return result;
    }
    
    private async Task<(ParkingSpot spot, User user)> ValidateParkingAsync(string userId, string spotId)
    {
        if (!Guid.TryParse(userId, out var userGuid)) throw new ArgumentException("User Id is not valid");

        if (!Guid.TryParse(spotId, out var spotGuid)) throw new ArgumentException("Parking spot Id is not valid");

        User? user;
        if ((user = await _database.Users.FindAsync(userGuid)) is null)
            throw new UnauthorizedException("User with current Id was not found");

        ParkingSpot? spot;
        if ((spot = await _database.ParkingSpots.FindAsync(spotGuid)) is null)
            throw new NotFoundException("Parking spot with current Id was not found");
        
        return (spot, user);
    }
}