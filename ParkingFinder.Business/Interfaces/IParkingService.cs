using ParkingFinder.Business.DTOs;
using ParkingFinder.Business.Exceptions;

namespace ParkingFinder.Business.Interfaces;

public interface IParkingService
{
    Task<IEnumerable<ParkingSpotInfo>> GetAllParkingSpots();
    Task<ParkingSpotInfo> GetParkingSpotById(string id);
    
    /// <summary>
    /// Adds new parking spot to the database
    /// </summary>
    /// <param name="info">Parking spot to add</param>
    /// <exception cref="ArgumentNullException">On parking spot is null</exception>
    Task AddParkingSpot(ParkingSpotInfo info);
    
    /// <summary>
    /// Deletes parking spot from the database
    /// </summary>
    /// <param name="guid">Id of the parking spot to delete</param>
    /// <exception cref="ArgumentException">On invalid parking spot id</exception>
    /// <exception cref="NotFoundException">On parking spot not found by id</exception>
    Task DeleteParkingSpot(string guid);

    /// <summary>
    /// Gives the best parking spot relative to the user
    /// </summary>
    /// <param name="cords">Coordinates of the user</param>
    /// <param name="userId">Id of the parked user</param>
    /// <param name="now">Current time</param>
    /// <returns>Information about the calculated parking spot</returns>
    /// <exception cref="ArgumentException">On invalid parking spot id</exception>
    /// <exception cref="UnauthorizedException">On user not found by id</exception>
    /// <exception cref="NotFoundException">On all parking spots occupied</exception>
    Task<ParkingSpotInfo> SuggestParkingSpot((decimal latitude, decimal longitude) cords, string userId, DateTime now);

    /// <summary>
    /// Records the log for the successful user parking
    /// </summary>
    /// <param name="userId">Id of the parked user</param>
    /// <param name="spotId">Id of the parking spot</param>
    /// <param name="now">Current time</param>
    /// <exception cref="OccupationException">On non-booked parking spot</exception>
    /// <exception cref="ArgumentException">On invalid parking spot or user id</exception>
    /// <exception cref="UnauthorizedException">On user not found by id</exception>
    /// <exception cref="NotFoundException">On parking spot not found by id</exception>
    Task ReportParking(string userId, string spotId, DateTime now);

    /// <summary>
    /// Records the log for the successful parking spot leaving 
    /// </summary>
    /// <param name="userId">Id of the parked user</param>
    /// <param name="spotId">Id of the parking spot</param>
    /// <param name="now">Current time</param>
    /// <exception cref="OccupationException">On parking spot not occupied</exception>
    /// <exception cref="NotFoundException">On parking spot not been registered for parking</exception>
    /// <exception cref="ArgumentException">On invalid parking spot or user id</exception>
    /// <exception cref="UnauthorizedException">On user not found by id</exception>
    /// <exception cref="NotFoundException">On parking spot not found by id</exception>
    Task ReportLeaving(string userId, string spotId, DateTime now);

    /// <summary>
    /// Records the log for the parking spot been marked as free but been occupied
    /// </summary>
    /// <param name="userId">Id of the parked user</param>
    /// <param name="spotId">Id of the parking spot</param>
    /// <param name="now">Current time</param>
    /// <exception cref="OccupationException">On parking spot already been marked as occupied</exception>
    /// <exception cref="ArgumentException">On invalid parking spot or user id</exception>
    /// <exception cref="UnauthorizedException">On user not found by id</exception>
    /// <exception cref="NotFoundException">On parking spot not found by id</exception>
    Task ReportBlocking(string userId, string spotId, DateTime now);

    /// <summary>
    /// Checks if the specific parking spot is occupied
    /// </summary>
    /// <param name="userId">Id of the parked user</param>
    /// <param name="spotId">Id of the parking spot</param>
    /// <param name="now">Current time</param>
    /// <returns>Information about the free parking spot</returns>
    /// <exception cref="ArgumentException">On invalid parking spot or user id</exception>
    /// <exception cref="UnauthorizedException">On user not found by id</exception>
    /// <exception cref="NotFoundException">On requested parking spot not found</exception>
    /// <exception cref="OccupationException">On requested parking spot been occupied</exception>
    Task<ParkingSpotInfo> CheckFavouriteSpot(string userId, string spotId, DateTime now);
    

}