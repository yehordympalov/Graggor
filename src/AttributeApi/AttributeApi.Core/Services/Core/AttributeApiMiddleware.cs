using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AttributeApi.Services.Core;

internal class AttributeApiMiddleware(ILogger<AttributeApiMiddleware> logger, RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var requestPath = context.Request.PathBase + context.Request.Path;
        var timestamp = Stopwatch.GetTimestamp();
        logger.LogInformation("Request {RequestPath} is received", requestPath);

        await next(context);

        logger.LogInformation("Request {RequestPath} execution has been finished in {ElapsedTime} ms", requestPath, Stopwatch.GetElapsedTime(timestamp).Milliseconds);
    }
}
