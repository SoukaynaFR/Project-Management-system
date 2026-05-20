using System.Text.Json;

namespace PMS.Api.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            message = exception.Message,
            statusCode = context.Response.StatusCode
        };

        switch (exception)
        {
            case UnauthorizedAccessException:
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                response = new { message = exception.Message, statusCode = 401 };
                break;
            case KeyNotFoundException:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                response = new { message = exception.Message, statusCode = 404 };
                break;
            case ArgumentException:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response = new { message = exception.Message, statusCode = 400 };
                break;
            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                response = new { message = "Une erreur interne est survenue", statusCode = 500 };
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(jsonResponse);
    }
}