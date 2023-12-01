using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParkingFinder.Business.Interfaces;
using ParkingFinder.Business.Services;
using ParkingFinder.Data;

namespace ParkingFinder.Business.Injections;

public static class ServiceCollectionExtensions
{
    public static void AddDatabase(this IServiceCollection services, string connection)
    {
        services.AddDbContext<Database>(opt => opt.UseSqlServer(connection));
    }
    
    public static void AddServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IParkingService, ParkingService>();
        services.AddHostedService<OccupationCalculationService>();
    }
}