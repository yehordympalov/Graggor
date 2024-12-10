using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AttributeApi.Core.Services.Builders;

internal static class ParametersBuilder
{
    internal static IServiceProvider _serviceProvider;
    internal static JsonSerializerOptions _options;

    public static object[] ResolveParameters(ILogger logger, HttpRequestData data, MethodInfo method)
    {
        var timestamp = Stopwatch.GetTimestamp();
        var parameters = method.GetParameters().ToList();
        var threadSafeList = new ConcurrentBag<ParameterInfo>(parameters);
        var lockObject = new Lock();
        var sortedParameters = new object[parameters.Count];

        Task.WaitAll(
            ProceedFromBodyParameter(ref lockObject, ref sortedParameters, data.Body, threadSafeList),
            ProceedFromRouteParameters(ref lockObject, ref sortedParameters, threadSafeList, data.RouteTemplate, data.RequestPath),
            ProceedFromServiceParameters(ref lockObject, ref sortedParameters, threadSafeList),
            ProceedFromQueryRouteParameters(ref lockObject, ref sortedParameters, threadSafeList, data.Query));

        var elapsedTime = Stopwatch.GetElapsedTime(timestamp).Milliseconds;
        logger.LogDebug("Parameters resolving took {ElapsedTime} ms", elapsedTime);

        return sortedParameters;
    }

    private static Task ProceedFromBodyParameter(ref Lock lockObject, ref object[] array, Stream body, ConcurrentBag<ParameterInfo> threadSafeList)
    {
        var fromBodyParameter = threadSafeList.SingleOrDefault(parameter => parameter.GetCustomAttribute<FromBodyAttribute>() is not null);

        if (fromBodyParameter is not null)
        {
            object resolvedParameter;

            if (_options.TryGetTypeInfo(fromBodyParameter.ParameterType, out var typeInfo))
            {
                resolvedParameter = JsonSerializer.Deserialize(body, typeInfo);
            }
            else
            {
                var buffer = new byte[4096];
                var chars = new char[body.ReadAsync(buffer).GetAwaiter().GetResult()];
                Encoding.UTF8.GetChars(buffer, 0, chars.Length, chars, 0);
                resolvedParameter = JsonSerializer.Deserialize(chars, fromBodyParameter.ParameterType, _options);
            }
            
            var index = threadSafeList.ToList().IndexOf(fromBodyParameter);

            lock (lockObject)
            {
                array[index] = resolvedParameter;
            }
        }

        return Task.CompletedTask;
    }

    private static Task ProceedFromServiceParameters(ref Lock lockObject, ref object[] array, ConcurrentBag<ParameterInfo> threadSafeList)
    {
        var fromServiceParameters = threadSafeList.Where(parameter => parameter.GetCustomAttribute<FromServicesAttribute>() is not null);

        var parameters = threadSafeList.ToList();

        foreach (var fromServiceParameter in fromServiceParameters)
        {
            var service = _serviceProvider.GetRequiredService(fromServiceParameter.ParameterType);
            var index = parameters.IndexOf(fromServiceParameter);

            lock (lockObject)
            {
                array[index] = service;
            }
        }

        return Task.CompletedTask;
    }

    private static Task ProceedFromRouteParameters(ref Lock lockObject, ref object[] array, ConcurrentBag<ParameterInfo> threadSafeList, string routeTemplate, string requestPath)
    {
        var routeSegments = routeTemplate.Trim('/').Split('/');
        var pathSegments = requestPath.Trim('/').Split('/');

        if (routeSegments.Length != pathSegments.Length)
        {
            throw new InvalidOperationException("Route and request path do not match.");
        }

        var routeParameters = new Dictionary<string, string>();

        for (var i = 0; i < routeSegments.Length; i++)
        {
            if (routeSegments[i].StartsWith("{") && routeSegments[i].EndsWith("}"))
            {
                var paramName = routeSegments[i].Trim('{', '}');
                routeParameters[paramName] = pathSegments[i];
            }
        }

        var fromRouteParameters = threadSafeList.Where(parameter => parameter.GetCustomAttribute<FromRouteAttribute>() is not null);
        var parameters = threadSafeList.ToList();

        foreach (var parameter in fromRouteParameters)
        {
            var index = parameters.IndexOf(parameter);

            if (routeParameters.TryGetValue(parameter.Name!, out var stringValue))
            {
                var value = Convert.ChangeType(stringValue, parameter.ParameterType);

                lock (lockObject)
                {
                    array[index] = value;
                }
            }
        }

        return Task.CompletedTask;
    }

    private static Task ProceedFromQueryRouteParameters(ref Lock lockObject, ref object[] array, ConcurrentBag<ParameterInfo> threadSafeList, Dictionary<string, string?> query)
    {
        var fromQueryParameters = threadSafeList.Where(parameter => parameter.GetCustomAttribute<FromQueryAttribute>() is not null);
        var parameters = threadSafeList.ToList();

        foreach (var parameter in fromQueryParameters)
        {
            var index = parameters.IndexOf(parameter);
            var parameterType = parameter.ParameterType;

            if (query.TryGetValue(parameter.Name!, out var json))
            {
                var resolvedParameter = _options.TryGetTypeInfo(parameterType, out var typeInfo)
                    ? JsonSerializer.Deserialize(json, typeInfo) : JsonSerializer.Deserialize(json, parameterType, _options);

                lock (lockObject)
                {
                    array[index] = resolvedParameter;
                }
            }
        }

        return Task.CompletedTask;
    }
}

internal record HttpRequestData(string RouteTemplate, string RequestPath, Stream Body, Dictionary<string, string?> Query);
