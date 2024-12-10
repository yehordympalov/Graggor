using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http;

namespace AttributeApi.Services.Core;
internal static class EndpointExecutor
{
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