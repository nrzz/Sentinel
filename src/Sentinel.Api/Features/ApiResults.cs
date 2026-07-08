using Microsoft.AspNetCore.Mvc;

namespace Sentinel.Api.Features;

internal static class ApiResults
{
    public static IResult ValidationProblem(IDictionary<string, string[]> errors)
    {
        return Results.ValidationProblem(errors, statusCode: StatusCodes.Status400BadRequest);
    }

    public static IResult Unauthorized(string detail)
    {
        return Results.Problem(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = detail
        });
    }

    public static IResult Forbidden(string detail)
    {
        return Results.Problem(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Detail = detail
        });
    }

    public static IResult NotFound(string detail)
    {
        return Results.Problem(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Not Found",
            Detail = detail
        });
    }

    public static IResult Conflict(string detail)
    {
        return Results.Problem(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Detail = detail
        });
    }

    public static IResult BadRequest(string detail)
    {
        return Results.Problem(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad Request",
            Detail = detail
        });
    }
}
