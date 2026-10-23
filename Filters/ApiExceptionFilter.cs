using DriveX.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DriveX.Filters;

// The ASP.NET Core version of GlobalExceptionHandler from the Spring app.
public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is AppException)
        {
            context.Result = new ObjectResult(new { message = context.Exception.Message })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }
        else
        {
            _logger.LogError(context.Exception, "Unhandled exception");
            context.Result = new ObjectResult(new { message = "Something went wrong" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        context.ExceptionHandled = true;
    }
}
