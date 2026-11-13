using DraftPuck.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;
using System.ComponentModel.DataAnnotations;

namespace DraftPuck.Web.Filters;

public class ApiExceptionFilterAttribute(
    ILogger<ApiExceptionFilterAttribute> logger,
    IWebHostEnvironment environment) : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        var exception = context.Exception;

        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            BadRequestException or ValidationException => (StatusCodes.Status400BadRequest, "Bad Request"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            _ => (StatusCodes.Status500InternalServerError, "An error occurred while processing your request.")
        };

        ProblemDetails details;
        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "An unhandled exception occurred.");

            details = environment.IsDevelopment()
                ? new ProblemDetails { Status = status, Title = $"Unhandled Exception: {exception.GetType().Name}", Detail = exception.ToString() }
                : new ProblemDetails { Status = status, Title = title, Detail = "An unexpected error occurred. Please try again." };
        }
        else
        {
            // Expected client errors (expired session, bad input, missing resource) are not failures
            // of the app, so log one line without a stack trace instead of an error.
            logger.LogInformation(
                "{StatusCode} {ExceptionType}: {Message}{InnerMessage}",
                status,
                exception.GetType().Name,
                exception.Message,
                exception.InnerException is { } inner ? $" ({inner.GetType().Name}: {inner.Message})" : "");

            details = new ProblemDetails { Status = status, Title = title, Detail = exception.Message };
        }

        context.Result = new ObjectResult(details) { StatusCode = status };
        context.ExceptionHandled = true;
        base.OnException(context);
    }
}
