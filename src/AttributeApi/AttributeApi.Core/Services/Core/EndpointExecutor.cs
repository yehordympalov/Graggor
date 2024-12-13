using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http;

namespace AttributeApi.Services.Core;

/// <summary>
/// Post-Processor for endpoint method
/// </summary>
internal static class EndpointExecutor
{
    /// <summary>
    /// Performs post-processing dependent on <paramref name="obj"/> type.
    /// </summary>
    /// <param name="context">Context of the current request.</param>
    /// <param name="obj">Result of the execution of the endpoint method.</param>
    /// <param name="typeInfo">Type information resolver in case of specific serialization.</param>
    /// <returns>Task to be awaited of the specific post-processing.</returns>
    public static Task ExecuteAsync(HttpContext context, object? obj, JsonTypeInfo<object> typeInfo)
    {
        if (obj is null)
        {
            context.Response.StatusCode = StatusCodes.Status200OK;

            return Task.CompletedTask;
        }

        if (obj is IResult result)
        {
            return result.ExecuteAsync(context);
        }

        if (obj is string stringValue)
        {
            context.Response.ContentType ??= "text/plain; charset=utf-8";

            return context.Response.WriteAsJsonAsync(stringValue, typeInfo);
        }

        return context.Response.WriteAsJsonAsync(obj);
    }
}
