using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ParkingFinder.Data;
using ParkingFinder.Data.Entities;

namespace ParkingFinder.Business.Services;

public class OccupationCalculationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OccupationCalculationService> _logger;
    private readonly OccupationCalculationLogic _logic;
    private readonly OccupationServiceProps _props;
    private readonly TimeSpan _delay;

    public OccupationCalculationService(ILogger<OccupationCalculationService> logger, IServiceProvider serviceProvider,
        IConfiguration cfg)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _delay = TimeSpan.FromMinutes(double.Parse(cfg["Parking:OccupationServiceDelay"]!));
        _logic = new OccupationCalculationLogic(_delay, int.Parse(cfg["Parking:MovingNumber"]!));
        _props = InitProps();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting occupation calculation service");
        var startDelay = (int)(_delay - _props.OnResumeTime).TotalMilliseconds;
        await Task.Delay(startDelay, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("Starting occupation calculation process");
            _props.StartTime = DateTime.Now;
            using var scope = _serviceProvider.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<Database>();
            
            await _logic.ReportTimeDurationAsync(database, _props.StartTime);
            await _logic.CalculateSimpleOccupationAsync(database, _props.TotalTime);

            _props.TotalTime += _delay;
            _props.FinishTime = DateTime.Now;
            _logger.LogInformation("Finished occupation calculation process");
            var delay = (int)(_delay - (_props.FinishTime - _props.StartTime)).TotalMilliseconds;
            await Task.Delay(delay < 0 ? 0 : delay, stoppingToken);
        }

        _props.OnResumeTime = DateTime.Now - _props.FinishTime;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping the occupation calculation service");
        
        using var scope = _serviceProvider.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<Database>();

        database.OccupationServiceProps.Update(_props);
        database.SaveChanges();
        
        _logger.LogInformation("Occupation calculation service props saved");
        return base.StopAsync(cancellationToken);
    }

    private OccupationServiceProps InitProps()
    {
        using var scope = _serviceProvider.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<Database>();

        if (database.OccupationServiceProps.Any())
            return database.OccupationServiceProps.First();

        var props = new OccupationServiceProps
        {
            TotalTime = TimeSpan.Zero,
            OnResumeTime = TimeSpan.Zero,
            StartTime = DateTime.Now,
            FinishTime = DateTime.Now
        };

        database.OccupationServiceProps.Add(props);
        database.SaveChanges();

        return props;
    }
}

public class OccupationCalculationLogic
{
    private readonly TimeSpan _delay;
    private readonly int _movingNumber;
    
    public OccupationCalculationLogic(TimeSpan delay, int movingNumber)
    {
        _delay = delay;
        _movingNumber = movingNumber;
    }
    
    public async Task ReportTimeDurationAsync(Database database, DateTime currentTime)
    {
        var spots = await database.ParkingSpots.ToListAsync();
        var times = new List<ParkingTime>(spots.Count);

        foreach (var spot in spots)
        {
            var logs = await database.ParkingLogs
                .Where(l => l.SpotId == spot.Id && l.Status != ParkingStatus.Booking)
                .ToListAsync();
            
            var duration = new TimeSpan(0);
            foreach (var log in logs)
            {
                switch (log.Status)
                {
                    case ParkingStatus.Entering:
                        duration += currentTime - log.EnterTime;
                        log.EnterTime = currentTime;
                        break;
                    
                    case ParkingStatus.Leaving:
                        duration += log.LeaveTime - log.EnterTime;
                        database.Remove(log);
                        break;
                    
                    case ParkingStatus.Unavailable:
                        if (log.LeaveTime - log.EnterTime > _delay)
                        {
                            duration += currentTime - log.EnterTime;
                            log.EnterTime = currentTime;
                            break;
                        }
                        
                        duration += log.LeaveTime - log.EnterTime;
                        database.Remove(log);
                        
                        spot.Occupied = false;
                        break;                        
                }

                await database.SaveChangesAsync();
            }

            var time = new ParkingTime(
                spotId: spot.Id,
                duration: duration,
                recordTime: currentTime
            );
            
            times.Add(time);
        }

        await database.ParkingTimes.AddRangeAsync(times);
        await database.SaveChangesAsync();
    }
    
    public async Task CalculateSimpleOccupationAsync(Database database, TimeSpan totalTime)
    {
        var spots = await database.ParkingSpots.ToListAsync();

        foreach (var spot in spots)
        {
            var time = await database.ParkingTimes
                .Where(t => t.SpotId == spot.Id)
                .OrderByDescending(t => t.RecordTime)
                .FirstOrDefaultAsync();
            
            var ratio = spot.OccupationRatio;

            spot.OccupationRatio = (ratio * totalTime.TotalMilliseconds + time?.Duration.TotalMilliseconds ?? 0) /
                                   (totalTime + _delay).TotalMilliseconds;
        }

        await database.SaveChangesAsync();
    }

    public async Task CalculateMovingOccupationAsync(Database database)
    {
        var spots = await database.ParkingSpots.ToListAsync();

        foreach (var spot in spots)
        {
            if (await database.ParkingTimes.CountAsync(t => t.SpotId == spot.Id) < _movingNumber)
                continue;
            
            spot.OccupationRatio = await database.ParkingTimes
                .Where(t => t.SpotId == spot.Id)
                .OrderByDescending(t => t.RecordTime)
                .Take(_movingNumber)
                .SumAsync(t => t.Duration.TotalMilliseconds) / (_movingNumber * _delay).TotalMilliseconds;
        }

        await database.SaveChangesAsync();
    }

    public async Task CalculateWeightedOccupationAsync(Database database)
    {
        var spots = await database.ParkingSpots.ToListAsync();

        foreach (var spot in spots)
        {
            if (await database.ParkingTimes.CountAsync(t => t.SpotId == spot.Id) < _movingNumber)
                continue;
            
            spot.OccupationRatio = await database.ParkingTimes
                .Where(t => t.SpotId == spot.Id)
                .OrderByDescending(t => t.RecordTime)
                .Take(_movingNumber)
                .Select((t, i) => new
                {
                    WeightedDuration = (_movingNumber - i) * t.Duration.TotalMilliseconds
                })
                .SumAsync(t => t.WeightedDuration) / (_movingNumber * (_movingNumber + 1) * _delay / 2.0).TotalMilliseconds;
        }

        await database.SaveChangesAsync();
    }
}