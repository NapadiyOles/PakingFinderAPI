using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ParkingFinder.Data;
using ParkingFinder.Data.Entities;

namespace ParkingFinder.Business.Services;

public class LogManagementService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OccupationCalculationService> _logger;
    private readonly LogManagementLogic _logic;
    private readonly TimeSpan _delay;
    private DateTime _startTime, _finishTime;

    public LogManagementService(ILogger<OccupationCalculationService> logger, IServiceProvider serviceProvider, IConfiguration cfg)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _delay = TimeSpan.FromMinutes(double.Parse(cfg["Parking:BookingServiceDelay"]!));
        _logic = new LogManagementLogic(_delay);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(_delay, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<Database>();

            _startTime = DateTime.Now;
            await _logic.DeleteBookingLogsAsync(database, _startTime);
            _finishTime = DateTime.Now;
            
            var delay = (int)(_delay - (_finishTime - _startTime)).TotalMilliseconds;
            await Task.Delay(delay < 0 ? 0 : delay, stoppingToken);
        }
    }
}

public class LogManagementLogic
{
    private readonly TimeSpan _delay;

    public LogManagementLogic(TimeSpan delay)
    {
        _delay = delay;
    }

    public async Task DeleteBookingLogsAsync(Database database, DateTime current)
    {
        var threshold = current - _delay;
        var logs = await database.ParkingLogs
            .Where(l => l.Status == ParkingStatus.Booking && l.EnterTime < threshold)
            .Include(l => l.Spot)
            .ToListAsync();

        foreach (var log in logs)
        {
            log.Spot.Occupied = false;
        }
        await database.SaveChangesAsync();

        database.ParkingLogs.RemoveRange(logs);
        await database.SaveChangesAsync();
    }

    public async Task DeleteExpiredBlockingLogs(Database database, DateTime current)
    {
        var threshold = current - _delay;
        var logs = await database.ParkingLogs
            .Where(l => l.Status == ParkingStatus.Unavailable && l.LeaveTime < threshold)
            .Include(l => l.Spot)
            .ToListAsync();

        foreach (var log in logs)
        {
            log.Spot.Occupied = false;
        }

        database.ParkingLogs.RemoveRange(logs);
        await database.SaveChangesAsync();
    }
}