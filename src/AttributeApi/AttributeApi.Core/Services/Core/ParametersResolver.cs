using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace AttributeApi.Core.Services.Core;

internal class ParametersResolver(IServiceProvider serviceProvider)
{
    public object[] ResolveParameters(MethodInfo method, JsonSerializerOptions options, HttpContextData data)
    {
        var parameters = method.GetParameters().ToList();
        var threadSafeList = new ConcurrentBag<ParameterInfo>(parameters);
        var lockObject = new Lock();
        var sortedParameters = new object[parameters.Count];

        Task.WaitAll(
            ProceedFromBodyParameter(ref lockObject, ref sortedParameters, options, threadSafeList, data._serializedBody),
            ProceedFromRouteParameters(ref lockObject, ref sortedParameters, threadSafeList, data._routeTemplate, data._requestPath),
            ProceedFromServiceParameters(ref lockObject, ref sortedParameters, threadSafeList),
            ProceedFromQueryRouteParameters(ref lockObject, ref sortedParameters, threadSafeList, data._query));

        return sortedParameters;
    }

    private static Task ProceedFromBodyParameter(ref Lock lockObject, ref object[] array, JsonSerializerOptions options, ConcurrentBag<ParameterInfo> threadSafeList, string body)
    {
        var fromBodyParameter = threadSafeList.SingleOrDefault(parameter => parameter.GetCustomAttribute<FromBodyAttribute>() is not null);

        if (fromBodyParameter is not null)
        {
            var deserializedBody = JsonSerializer.Deserialize(body, fromBodyParameter.ParameterType, options);
            var index = threadSafeList.ToList().IndexOf(fromBodyParameter);

            lock (lockObject)
            {
                array[index] = deserializedBody;
            }
        }

        return Task.CompletedTask;
    }

    private Task ProceedFromServiceParameters(ref Lock lockObject, ref object[] array, ConcurrentBag<ParameterInfo> threadSafeList)
    {
        var fromServiceParameters = threadSafeList.Where(parameter => parameter.GetCustomAttribute<FromServicesAttribute>() is not null);

        var parameters = threadSafeList.ToList();

        foreach (var fromServiceParameter in fromServiceParameters)
        {
            var service = serviceProvider.GetRequiredService(fromServiceParameter.ParameterType);
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

            if (query.TryGetValue(parameter.Name!, out var stringValue))
            {
                object value;

                try
                {
                    value = JsonSerializer.Deserialize(stringValue, parameterType);
                }
                catch
                {
                    value = Convert.ChangeType(stringValue, parameterType);
                }

                lock (lockObject)
                {
                    array[index] = value;
                }
            }
        }

        return Task.CompletedTask;
    }
}

internal readonly struct HttpContextData(string routeTemplate, string requestPath, string serializedBody, Dictionary<string, string?> query)
{
    internal readonly string _routeTemplate = routeTemplate;

    internal readonly string _requestPath = requestPath;

    internal readonly string _serializedBody = serializedBody;

    internal readonly Dictionary<string, string?> _query = query;
}
