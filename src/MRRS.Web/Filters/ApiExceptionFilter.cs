using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MRRS.Web.Services;

namespace MRRS.Web.Filters;

public class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var (status, message) = context.Exception switch
        {
            BookingConflictException ex => (StatusCodes.Status409Conflict, ex.Message),
            InvalidBookingException ex => (StatusCodes.Status400BadRequest, ex.Message),
            NotFoundException ex => (StatusCodes.Status404NotFound, ex.Message),
            ForbiddenException ex => (StatusCodes.Status403Forbidden, ex.Message),
            _ => (0, null)
        };

        if (message is null)
        {
            return;
        }

        context.Result = new ObjectResult(new { message }) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
