using System.Net;

namespace SchoolEduERP.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            // Only redirect to NotFound page for GET requests that returned 404
            // and whose response hasn't already started (no redirect in progress)
            if (context.Response.StatusCode == (int)HttpStatusCode.NotFound
                && !context.Response.HasStarted
                && context.Request.Method == "GET"
                && !context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.Redirect("/Home/NotFound");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred. Path: {Path}", context.Request.Path);

            // In Development, rethrow so the Developer Exception Page shows full details
            if (_env.IsDevelopment())
                throw;

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 500;
                context.Response.Redirect("/Home/Error");
            }
        }
    }
}
