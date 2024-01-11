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
        _logic = new OccupationCalculationLogic(_delay, int.Parse(cfg["Parking:SpotsToTake"]!));
        _props = InitProps();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting occupation calculation service");
        _props.FinishTime = DateTime.Now - TimeSpan.FromTicks(_props.OnResumeTime);
        var startDelay = (int)(_delay - TimeSpan.FromTicks(_props.OnResumeTime)).TotalMilliseconds;
        await Task.Delay(startDelay > 0 ? startDelay : 0, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            _props.StartTime = DateTime.Now;
            _logger.LogWarning("Starting occupation calculation process");
            using var scope = _serviceProvider.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<Database>();
            
            _props.TotalTime += _delay.Ticks;
            await _logic.ReportTimeDurationAsync(database, _props.StartTime);
            await _logic.CalculateSimpleOccupationAsync(database, TimeSpan.FromTicks(_props.TotalTime));
            _props.FinishTime = DateTime.Now;

            _logger.LogInformation("Finished occupation calculation process");
            var delay = (int)(_delay - (_props.FinishTime - _props.StartTime)).TotalMilliseconds;
            await Task.Delay(delay > 0 ? delay : 0, stoppingToken);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping the occupation calculation service");
        
        using var scope = _serviceProvider.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<Database>();

        _props.OnResumeTime = (DateTime.Now - _props.FinishTime).Ticks;
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
            TotalTime = 0,
            OnResumeTime = 0,
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
    private readonly int _spotsToTake;
    
    public OccupationCalculationLogic(TimeSpan delay, int spotsToTake)
    {
        _delay = delay;
        _spotsToTake = spotsToTake;
    }
    
    public async Task ReportTimeDurationAsync(Database database, DateTime currentTime)
    {
        var spots = await database.ParkingSpots.ToListAsync();

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
                    {
                        if (log.LeaveTime - log.EnterTime > _delay)
                        {
                            duration += currentTime - log.EnterTime;
                            log.EnterTime = currentTime;
                            break;
                        }
                        
                        if (log.LeaveTime > log.EnterTime)
                            duration += log.LeaveTime - log.EnterTime;
                        
                        database.ParkingLogs.Remove(log);
                        spot.Occupied = false;
                        break;
                    }

                    case ParkingStatus.Leaving:
                    {
                        if(log.LeaveTime > log.EnterTime)
                            duration += log.LeaveTime - log.EnterTime;
                        
                        database.ParkingLogs.Remove(log);
                        break;
                    }

                    case ParkingStatus.Unavailable:
                    {
                        if (log.LeaveTime - log.EnterTime > _delay)
                        {
                            duration += currentTime - log.EnterTime;
                            log.EnterTime = currentTime;
                            break;
                        }

                        if (log.LeaveTime > log.EnterTime)
                            duration += log.LeaveTime - log.EnterTime;

                        database.ParkingLogs.Remove(log);
                        spot.Occupied = false;
                        break;
                    }
                }
            }

            // if (duration > _delay) duration = _delay;
            
            var time = new ParkingTime(
                spotId: spot.Id,
                duration: duration.Ticks,
                recordTime: currentTime
            );
            
            await database.ParkingTimes.AddAsync(time);
        }
        
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
            spot.OccupationRatio = (ratio * totalTime.TotalSeconds + TimeSpan.FromTicks(time?.Duration ?? 0).TotalSeconds) /
                                    (totalTime + _delay).TotalSeconds;
        }

        await database.SaveChangesAsync();
    }

    public async Task CalculateMovingOccupationAsync(Database database)
    {
        var spots = await database.ParkingSpots.ToListAsync();

        foreach (var spot in spots)
        {
            var times = await TakeParkingTimes(database, spot);
            var sum = times.Sum(t => TimeSpan.FromTicks(t.Duration).TotalSeconds);
            spot.OccupationRatio = times.Count > 0 ? sum / (times.Count * _delay.TotalSeconds) : 0.5;
        }
        
        await database.SaveChangesAsync();
    }

    public async Task CalculateWeightedOccupationAsync(Database database)
    {
        var spots = await database.ParkingSpots.ToListAsync();

        foreach (var spot in spots)
        {
            var times = await TakeParkingTimes(database, spot);
            var sum = times
                .Select((t, i) => new
                    { WeightedDuration = (times.Count - i) * TimeSpan.FromTicks(t.Duration).TotalSeconds })
                .Sum(t => t.WeightedDuration);
            spot.OccupationRatio =
                times.Count > 0 ? sum / (times.Count * (times.Count + 1) * _delay.TotalSeconds / 2.0) : 0.5;
        }

        await database.SaveChangesAsync();
    }

    private Task<List<ParkingTime>> TakeParkingTimes(Database database, ParkingSpot spot)
    {
        var times = database.ParkingTimes.Where(t => t.SpotId == spot.Id);
        var ordered = times.OrderByDescending(t => t.RecordTime)
            .Take(_spotsToTake)
            .ToListAsync();

        return ordered;
    }
}