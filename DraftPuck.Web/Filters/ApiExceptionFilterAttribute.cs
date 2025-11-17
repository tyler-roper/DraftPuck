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
        logger.LogError(context.Exception, "An unhandled exception occurred.");

        if (context.Exception is NotFoundException)
        {
            var details = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = context.Exception.Message
            };
            context.Result = new NotFoundObjectResult(details);
        }
        else if (context.Exception is UnauthorizedException)
        {
            var details = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = context.Exception.Message
            };
            context.Result = new ObjectResult(details) { StatusCode = StatusCodes.Status401Unauthorized };
        }
        else if (context.Exception is BadRequestException || context.Exception is ValidationException)
        {
            var details = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = context.Exception.Message
            };
            context.Result = new BadRequestObjectResult(details);
        }
        else if (context.Exception is ForbiddenException)
        {
            var details = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = context.Exception.Message
            };
            context.Result = new ObjectResult(details) { StatusCode = StatusCodes.Status403Forbidden };
        }
        else
        {
            var details = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred while processing your request."
            };

            if (environment.IsDevelopment())
            {
                details.Title = $"Unhandled Exception: {context.Exception.GetType().Name}";
                details.Detail = context.Exception.ToString();
            }
            else
            {
                details.Detail = "An unexpected error occurred. Please try again.";
            }

            context.Result = new ObjectResult(details) { StatusCode = StatusCodes.Status500InternalServerError };
        }

        context.ExceptionHandled = true;
        base.OnException(context);
    }
}