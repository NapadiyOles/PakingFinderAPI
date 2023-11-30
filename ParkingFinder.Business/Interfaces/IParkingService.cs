using ParkingFinder.Business.DTOs;

namespace ParkingFinder.Business.Interfaces;

public interface IParkingService
{
    Task<IEnumerable<ParkingSpotInfo>> GetAllParkingSpots();
    Task AddParkingSpot(ParkingSpotInfo info);
    Task DeleteParkingSpot(string guid);
    Task<ParkingSpotInfo> SuggestParkingSpot((decimal latitude, decimal longitude) cords, string userId);
    Task ReportParking(string userId, string spotId);
    Task ReportLeaving(string userId, string spotId);
    Task ReportBlocking(string userId, string spotId);
    Task<ParkingSpotInfo> CheckFavouriteSpot(string userId, string spotId);
    

}