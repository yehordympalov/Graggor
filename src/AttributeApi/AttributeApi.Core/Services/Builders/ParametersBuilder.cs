using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AttributeApi.Services.Builders;

/// <summary>
/// Specific builder which resolves all parameters for chosen method of the endpoint
/// </summary>
internal static class ParametersBuilder
{
    /// <summary>
    /// Type resolver for route patterns
    /// </summary>
    internal static readonly ConcurrentDictionary<string, Func<string, object>> _routeTypeResolvers = new();
    internal static IServiceProvider _serviceProvider;
    internal static JsonSerializerOptions _options;

    static ParametersBuilder()
    {
        _routeTypeResolvers.TryAdd("guid", body => Guid.Parse(body));
        _routeTypeResolvers.TryAdd("string", body => body);
        _routeTypeResolvers.TryAdd("int", body => Convert.ToInt32(body));
        _routeTypeResolvers.TryAdd("int64", body => Convert.ToInt64(body));
        _routeTypeResolvers.TryAdd("int128", body => Int128.Parse(body));
        _routeTypeResolvers.TryAdd("double", body => Convert.ToDouble(body));
        _routeTypeResolvers.TryAdd("decimal", body => Convert.ToDecimal(body));
    }

    /// <summary>
    /// Takes the all information about incoming request and endpoint's method
    /// and resolves all parameters for this method.
    /// </summary>
    /// <param name="data">Data to resolve parameters for the current request</param>
    /// <returns>Sorted array of <see cref="object"/> which contains all possible instances
    /// which are predicted as a target method parameters</returns>
    public static async Task<object?[]> ResolveParametersAsync(HttpRequestData data)
    {
        var count = data.Parameters.Count;

        if (count is 0)
        {
            return [];
        }

        var lockObject = new Lock();
        var sortedInstances = new object[count];
        var parameterTask = ProceedFromBodyParameter(data.Parameters, data.Body);

        await Task.WhenAll(parameterTask,
            ProceedFromRouteParameters(ref lockObject, ref sortedInstances, data.Parameters, data.RouteTemplate, data.RequestPath),
            ProceedFromServiceParameters(ref lockObject, ref sortedInstances, data.Parameters),
            ProceedFromQueryRouteParameters(ref lockObject, ref sortedInstances, data.Parameters, data.Query));

        var resolvedBody = parameterTask.Result;

        if (resolvedBody != ResolvedParameter._empty)
        {
            sortedInstances[resolvedBody.Index] = resolvedBody.Instance;
        }

        return sortedInstances;
    }

    /// <summary>
    /// Resolves parameter with attribute <see cref="FromBodyAttribute"/>.
    /// If method contains more than 1 parameter with this attribute, it will throw <see cref="InvalidOperationException"/>.
    /// If method expects parameter, but body does not contain it - null value will be passed.
    /// In case of exception in deserialization - exception will be thrown.
    /// </summary>
    /// <param name="parameters">Information about parameters which are expected to be passed into the target method</param>
    /// <param name="body">Instance of the <see cref="Stream"/> which is brought with the current request</param>
    /// <returns>New created instance of <see cref="ResolvedParameters"/> in case of successful extracting; otherwise - empty instance.</returns>
    /// <exception cref="JsonException">In case of exception during the deserialization.</exception>
    /// <exception cref="InvalidOperationException">In case of multiple using of <see cref="FromBodyAttribute"/>.</exception>
    private static async Task<ResolvedParameter> ProceedFromBodyParameter(List<ParameterInfo> parameters, Stream body)
    {
        var fromBodyParameter = parameters.SingleOrDefault(parameter => parameter.GetCustomAttribute<FromBodyAttribute>() is not null);

        if (fromBodyParameter != null)
        {
            object? resolvedParameter;

            if (body.CanSeek)
            {
                if (body.Length == 0)
                {
                    return ResolvedParameter._empty;
                }

                if (_options.TryGetTypeInfo(fromBodyParameter.ParameterType, out var typeInfo))
                {
                    resolvedParameter = await JsonSerializer.DeserializeAsync(body, typeInfo).ConfigureAwait(false);
                }
                else
                {
                    resolvedParameter = await JsonSerializer.DeserializeAsync(body, fromBodyParameter.ParameterType, _options).ConfigureAwait(false);
                }
            }
            else
            {
                var buffer = new byte[4096];
                var count = await body.ReadAsync(buffer).ConfigureAwait(false);

                if (count == 0)
                {
                    return ResolvedParameter._empty;
                }

                var charBuffer = new char[count];
                Encoding.UTF8.GetChars(buffer, 0, count, charBuffer, 0);

                if (_options.TryGetTypeInfo(fromBodyParameter.ParameterType, out var typeInfo))
                {
                    resolvedParameter = JsonSerializer.Deserialize(charBuffer, typeInfo);
                }
                else
                {
                    resolvedParameter = JsonSerializer.Deserialize(charBuffer, fromBodyParameter.ParameterType, _options);
                }
            }

            var index = parameters.IndexOf(fromBodyParameter);

            return new ResolvedParameter(resolvedParameter, index);
        }

        return ResolvedParameter._empty;
    }

    /// <summary>
    /// Resolves parameters with attribute <see cref="FromServicesAttribute"/>
    /// </summary>
    /// <param name="lockObject">Instance to lock access to <paramref name="sortedInstances"/></param>
    /// <param name="sortedInstances">Reference to placement in memory where instance of sorted array is placed</param>
    /// <param name="parameters">Information about parameters which are expected to be passed into the target method</param>
    /// <returns>Completed task in case of success; otherwise - throws <see cref="InvalidOperationException"/></returns>
    /// <exception cref="InvalidOperationException">Thrown in case of not registered service in the dependency injection</exception>
    private static Task ProceedFromServiceParameters(ref Lock lockObject, ref object?[] sortedInstances, List<ParameterInfo> parameters)
    {
        var fromServiceParameters = parameters.Where(parameter => parameter.GetCustomAttribute<FromServicesAttribute>() is not null);

        foreach (var fromServiceParameter in fromServiceParameters)
        {
            var resolvedParameter = _serviceProvider.GetRequiredService(fromServiceParameter.ParameterType);
            var index = parameters.IndexOf(fromServiceParameter);

            lock (lockObject)
            {
                sortedInstances[index] = resolvedParameter;
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Resolves parameters with attribute <see cref="FromRouteAttribute"/>.
    /// </summary>
    /// <param name="lockObject">Instance to lock access to <paramref name="sortedInstances"/>.</param>
    /// <param name="sortedInstances">Reference to placement in memory where instance of sorted array is placed.</param>
    /// <param name="parameters">Information about parameters which are expected to be passed into the target method.</param>
    /// <param name="routeTemplate">Template of the current endpoint to be parsed with values if it's predicted.</param>
    /// <param name="requestPath">Full path of the current request.</param>
    /// <returns>Completed task in case of success; otherwise - throws an exception.</returns>
    /// <exception cref="InvalidOperationException">Thrown in case of negative match of <paramref name="routeTemplate"/> and <paramref name="requestPath"/>
    /// or there are more than 1 bind type in <paramref name="routeTemplate"/> for this instance.</exception>
    /// <exception cref="ArgumentException">Thrown in case of impossibility of resolving type of bind <paramref name="routeTemplate"/></exception>
    private static Task ProceedFromRouteParameters(ref Lock lockObject, ref object?[] sortedInstances, List<ParameterInfo> parameters, string routeTemplate, string requestPath)
    {
        if (!routeTemplate.Contains("{"))
        {
            return Task.CompletedTask;
        }

        var routeSegments = routeTemplate.Trim('/').Split('/');
        var pathSegments = requestPath.Trim('/').Split('/');

        if (routeSegments.Length != pathSegments.Length)
        {
            throw new InvalidOperationException("Route and request path do not match.");
        }

        var routeParameters = new Dictionary<string, RouteParameter>();

        for (var i = 0; i < routeSegments.Length; i++)
        {
            if (routeSegments[i].StartsWith("{") && routeSegments[i].EndsWith("}"))
            {
                var split = routeSegments[i].Trim('{', '}').Split(':');
                var parameterName = split[0];
                Func<string, object>? parameterType = null;

                if (split.Length == 2)
                {
                    var parameterTypeString = split[1];

                    parameterType = _routeTypeResolvers.TryGetValue(parameterTypeString, out var type) ? type
                        : throw new ArgumentException($"Cannot resolve type {parameterTypeString} for route parameter {parameterName}");
                }
                else if (split.Length > 2)
                {
                    throw new InvalidOperationException($"Route pattern cannot have more than 1 related types. Please verify attributes for your endpoint {routeTemplate}");
                }

                routeParameters[parameterName] = new RouteParameter(pathSegments[i], parameterType);
            }
        }

        var fromRouteParameters = parameters.Where(parameter => parameter.GetCustomAttribute<FromRouteAttribute>() is not null);

        foreach (var parameter in fromRouteParameters)
        {
            var index = parameters.IndexOf(parameter);

            if (routeParameters.TryGetValue(parameter.Name!, out var routeParameter))
            {
                var resolvedParameter = routeParameter.Resolver is not null
                    ? routeParameter.Resolver.Invoke(routeParameter.Value)
                    : Convert.ChangeType(routeParameter.Value, parameter.ParameterType);

                lock (lockObject)
                {
                    sortedInstances[index] = resolvedParameter;
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Resolves parameters with attribute <see cref="FromQueryAttribute"/>.
    /// </summary>
    /// <param name="lockObject">Instance to lock access to <paramref name="sortedInstances"/>.</param>
    /// <param name="sortedInstances">Reference to placement in memory where instance of sorted array is placed.</param>
    /// <param name="parameters">Information about parameters which are expected to be passed into the target method.</param>
    /// <param name="query">An instance of <see cref="Dictionary{TKey, TValue}"/> which contains all data which came in a query section of the current request.</param>
    /// <returns>Completed task in case of success; otherwise - throws an exception.</returns>
    /// <exception cref="JsonException">In case of exception during the deserialization.</exception>
    private static Task ProceedFromQueryRouteParameters(ref Lock lockObject, ref object?[] sortedInstances, List<ParameterInfo> parameters, Dictionary<string, string?> query)
    {
        var fromQueryParameters = parameters.Where(parameter => parameter.GetCustomAttribute<FromQueryAttribute>() is not null);

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
                    sortedInstances[index] = resolvedParameter;
                }
            }
        }

        return Task.CompletedTask;
    }

    private record ResolvedParameter(object? Instance, int Index)
    {
        internal static readonly ResolvedParameter _empty = new(null, -1);
    }

    private record struct RouteParameter(string Value, Func<string, object>? Resolver);
}
