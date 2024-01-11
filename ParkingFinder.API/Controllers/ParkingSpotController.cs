using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingFinder.API.Filters;
using ParkingFinder.API.Models;
using ParkingFinder.Business.DTOs;
using ParkingFinder.Business.Interfaces;
using ParkingFinder.Business.Utils;

namespace ParkingFinder.API.Controllers;

[ApiController]
[Route("parking")]
[ExceptionFilter]
[Authorize]
public class ParkingSpotController : ControllerBase
{
    private readonly IParkingService _service;
    private readonly ILogger<ParkingSpotController> _logger;

    public ParkingSpotController(IParkingService service, ILogger<ParkingSpotController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<IEnumerable<ParkingSpotModel>>> GetAllSpots()
    {
        var list = await _service.GetAllParkingSpots();

        var result = list.Select(e =>
            new ParkingSpotModel(
                id: e.Id,
                latitude: e.Latitude,
                longitude: e.Longitude
            )
        );
        
        return Ok(result);
    }
    
    [HttpPost("add")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult> AddParkingSpot(Coordinates cords)
    {
        var spot = new ParkingSpotInfo(
            default,
            latitude: cords.Latitude,
            longitude: cords.Longitude
        );

        await _service.AddParkingSpot(spot);
        
        return Ok();
    }

    [HttpDelete("{guid}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult> RemoveParkingSpot(string guid)
    {
        await _service.DeleteParkingSpot(guid);
        return Ok();
    }

    [HttpPut("suggest")]
    [Authorize(Roles = Role.User)]
    public async Task<ActionResult<ParkingSpotModel>> SuggestParkingSpot(UserCoordinates cords)
    {
        var spot = await _service.SuggestParkingSpot((cords.Latitude, cords.Longitude), cords.UserId, DateTime.Now);
        
        var result = new ParkingSpotModel(
            id: spot.Id,
            latitude: spot.Latitude,
            longitude: spot.Longitude
        );
        
        return Ok(result);
    }

    [HttpPut("favourite")]
    [Authorize(Roles = Role.User)]
    public async Task<ActionResult<ParkingSpotModel>> CheckFavourite(ReportModel report)
    {
        var spot = await _service.CheckFavouriteSpot(report.UserId, report.SpotId, DateTime.Now);

        var result = new ParkingSpotModel(
            id: spot.Id,
            latitude: spot.Latitude,
            longitude: spot.Longitude
        );

        return Ok(result);
    }

    [HttpPost("enter")]
    [Authorize(Roles = Role.User)]
    public async Task<ActionResult<Result>> ReportEntering(ReportModel report)
    {
        await _service.ReportParking(report.UserId, report.SpotId, DateTime.Now);
        return Ok(new Result("Entering"));
    } 
    
    [HttpPost("leave")]
    [Authorize(Roles = Role.User)]
    public async Task<ActionResult<Result>> ReportLeaving(ReportModel report)
    {
        await _service.ReportLeaving(report.UserId, report.SpotId, DateTime.Now);
        return Ok(new Result("Leaving"));
    } 
    
    [HttpPost("block")]
    [Authorize(Roles = Role.User)]
    public async Task<ActionResult<Result>> ReportBlocking(ReportModel report)
    {
        await _service.ReportBlocking(report.UserId, report.SpotId, DateTime.Now);
        return Ok(new Result("Blocking"));
    } 
}