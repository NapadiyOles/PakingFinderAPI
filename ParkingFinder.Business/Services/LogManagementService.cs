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
            _startTime = DateTime.Now;
            using var scope = _serviceProvider.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<Database>();

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

    public async Task DeleteBookingLogsAsync(Database database, DateTime currentTime)
    {
        await database.ParkingLogs
            .Where(l => l.Status == ParkingStatus.Booking && currentTime - l.EnterTime > _delay)
            .ExecuteDeleteAsync();
    }
}