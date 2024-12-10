using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AttributeApi.Services.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AttributeApi.Services.Builders;

internal static class EndpointRequestDelegateBuilder
{
    internal static JsonSerializerOptions _options;

    public static RequestDelegate CreateRequestDelegate(ILogger logger, object instance, MethodInfo method, string httpMethod, string routeTemplate)
    {
        return RequestDelegate;

        async Task RequestDelegate(HttpContext context)
        {
            var timestamp = Stopwatch.GetTimestamp();
            var request = context.Request;
            var requestPath = request.PathBase + request.Path;
            logger.LogInformation("Request {RequestPath} is received", requestPath);
            var query = request.Query.Count is not 0 ? request.Query.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.FirstOrDefault()) : [];
            var httpRequestData = new HttpRequestData(routeTemplate, requestPath, request.Body, query);
            var methodParameters = ParametersBuilder.ResolveParameters(logger, httpRequestData, method);
            var elapsedTime = Stopwatch.GetElapsedTime(timestamp).Milliseconds;
            logger.LogDebug("Parameters resolving took {ElapsedTime} ms", elapsedTime);

            object? result = await (dynamic)method.Invoke(instance, methodParameters);
            await EndpointExecutor.ExecuteAsync(context, result, (JsonTypeInfo<object>)_options.GetTypeInfo(typeof(object)));
            elapsedTime = Stopwatch.GetElapsedTime(timestamp).Milliseconds;
            logger.LogInformation("Request {Request} execution has been finished in {ElapsedTime} ms", requestPath, elapsedTime);
        }
    }
}
