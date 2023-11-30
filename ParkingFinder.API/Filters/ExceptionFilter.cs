using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ParkingFinder.Business.Exceptions;

namespace ParkingFinder.API.Filters;

public class ExceptionFilter : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        context.Result = context.Exception switch
        {
            ArgumentNullException => new NotFoundObjectResult(context.Exception.Message),
            NotFoundException => new NotFoundObjectResult(context.Exception.Message),
            ArgumentException => new BadRequestObjectResult(context.Exception.Message),
            AuthException => new BadRequestObjectResult(context.Exception.Message),
            UnauthorizedException => new UnauthorizedObjectResult(context.Exception.Message),
            RoleException => new ConflictObjectResult(context.Exception.Message),
            OccupationException => new ConflictObjectResult(context.Exception.Message),
            _ => new BadRequestObjectResult(
                $"Unhandled error occured. {context.Exception}: {context.Exception.Message}")
        };
        base.OnException(context);
    }
}