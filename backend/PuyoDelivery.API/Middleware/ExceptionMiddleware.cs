namespace PuyoDelivery.API.Middleware;

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
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var isDev = context.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true;
            await context.Response.WriteAsJsonAsync(new
            {
                isSuccess = false,
                error = isDev ? ex.Message : "Internal server error",
                code = "INTERNAL_ERROR"
            });
        }
    }
}
