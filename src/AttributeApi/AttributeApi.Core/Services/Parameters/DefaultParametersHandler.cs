using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;
using AttributeApi.Services.Builders;
using AttributeApi.Services.Parameters.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Services.Parameters;

/// <summary>
/// Default parameters binder.
/// </summary>
internal class DefaultParametersHandler(IServiceProvider serviceProvider, JsonSerializerOptions options) : IParametersHandler
{
    private readonly Type _enumerableType = typeof(IEnumerable);
    private readonly Type _listType = typeof(List<>);

    /// <summary>
    /// Type resolver for route patterns
    /// </summary>
    internal static readonly ConcurrentDictionary<string, Func<string, object>> _typeResolvers = new();

    static DefaultParametersHandler()
    {
        _typeResolvers.TryAdd("guid", body => Guid.Parse(body));
        _typeResolvers.TryAdd("string", body => body);
        _typeResolvers.TryAdd("int", body => Convert.ToInt32(body));
        _typeResolvers.TryAdd("int64", body => Convert.ToInt64(body));
        _typeResolvers.TryAdd("int128", body => Int128.Parse(body));
        _typeResolvers.TryAdd("double", body => Convert.ToDouble(body));
        _typeResolvers.TryAdd("decimal", body => Convert.ToDecimal(body));
    }

    public JsonSerializerOptions Options { get; } = options;

    public async Task<object?[]> HandleParametersAsync(HttpRequestData data)
    {
        var count = data.Parameters.Count;

        if (count is 0)
        {
            return [];
        }

        var lockObject = new Lock();
        var sortedInstances = new object[count];
        var parameterTask = BindFromBodyParameter(data.Parameters, data.Body);

        await Task.WhenAll(parameterTask,
            BindFromRouteParameters(ref lockObject, ref sortedInstances, data.Parameters, data.RouteTemplate, data.RequestPath),
            BindFromServiceParameters(ref lockObject, ref sortedInstances, data.Parameters),
            BindFromQueryRouteParameters(ref lockObject, ref sortedInstances, data.Parameters, data.QueryCollection));

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
    private async Task<ResolvedParameter> BindFromBodyParameter(List<ParameterInfo> parameters, Stream body)
    {
        var fromBodyParameter = parameters.SingleOrDefault(parameter => parameter.GetCustomAttribute<FromBodyAttribute>() is not null);

        if (fromBodyParameter != null)
        {
            var index = parameters.IndexOf(fromBodyParameter);
            object? resolvedParameter;

            if (body.CanSeek)
            {
                if (body.Length == 0)
                {
                    return ResolvedParameter.GetDefaultValue(fromBodyParameter, index);
                }

                if (Options.TryGetTypeInfo(fromBodyParameter.ParameterType, out var typeInfo))
                {
                    resolvedParameter = await JsonSerializer.DeserializeAsync(body, typeInfo).ConfigureAwait(false);
                }
                else
                {
                    resolvedParameter = await JsonSerializer.DeserializeAsync(body, fromBodyParameter.ParameterType, Options).ConfigureAwait(false);
                }
            }
            else
            {
                var buffer = new byte[4096];
                var count = await body.ReadAsync(buffer).ConfigureAwait(false);

                if (count == 0)
                {
                    return ResolvedParameter.GetDefaultValue(fromBodyParameter, index);
                }

                var charBuffer = new char[count];
                Encoding.UTF8.GetChars(buffer, 0, count, charBuffer, 0);

                if (Options.TryGetTypeInfo(fromBodyParameter.ParameterType, out var typeInfo))
                {
                    resolvedParameter = JsonSerializer.Deserialize(charBuffer, typeInfo);
                }
                else
                {
                    resolvedParameter = JsonSerializer.Deserialize(charBuffer, fromBodyParameter.ParameterType, Options);
                }
            }

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
    private Task BindFromServiceParameters(ref Lock lockObject, ref object?[] sortedInstances, List<ParameterInfo> parameters)
    {
        var fromServiceParameters = parameters.Where(parameter => parameter.GetCustomAttribute<FromServicesAttribute>() is not null);

        foreach (var fromServiceParameter in fromServiceParameters)
        {
            var resolvedParameter = serviceProvider.GetRequiredService(fromServiceParameter.ParameterType);
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
    private Task BindFromRouteParameters(ref Lock lockObject, ref object?[] sortedInstances, List<ParameterInfo> parameters, string routeTemplate, string requestPath)
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

                    parameterType = _typeResolvers.TryGetValue(parameterTypeString, out var type) ? type
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
    /// <param name="queryCollection">An instance of <see cref="IQueryCollection"/> which contains all data which came in a query section of the current request.</param>
    /// <returns>Completed task in case of success; otherwise - throws an exception.</returns>
    /// <exception cref="JsonException">In case of exception during the deserialization.</exception>
    private Task BindFromQueryRouteParameters(ref Lock lockObject, ref object?[] sortedInstances, List<ParameterInfo> parameters, IQueryCollection queryCollection)
    {
        var fromQueryParameters = parameters.Where(parameter => parameter.GetCustomAttribute<FromQueryAttribute>() is not null);

        foreach (var parameter in fromQueryParameters)
        {
            var index = parameters.IndexOf(parameter);
            object? resolvedParameter;

            // verifying if there is any values which we are expecting.
            if (queryCollection.TryGetValue(parameter.Name, out var stringValues))
            {
                // if value exists, and it's empty, we are trying insert a default value.
                if (stringValues.Count == 0)
                {
                    lock (lockObject)
                    {
                        sortedInstances[index] = parameter.HasDefaultValue ? parameter.DefaultValue : null;
                    }

                    continue;
                }

                Type? element;
                var returnType = parameter.ParameterType;
                var isArray = returnType.IsArray;

                // verifying the behavior for the parameter after the resolving 
                // all of his objects.
                if (isArray)
                {
                    element = returnType.GetElementType();
                }
                else if (returnType.IsAssignableTo(_enumerableType))
                {
                    element = returnType.GetGenericArguments().First();
                }
                // if parameter is not an array or enumerable, we try to convert type of single element;
                // and then continue iteration.
                else
                {
                    resolvedParameter = Convert.ChangeType(stringValues[0], returnType);

                    lock (lockObject)
                    {
                        sortedInstances[index] = resolvedParameter;
                    }

                    continue;
                }

                var temp = new List<object>(stringValues.Count);

                // for performance purposes, we allocate memory for additional delegate
                // to resolve object type.
                Action<string> action = _typeResolvers.TryGetValue(element.Name.ToLowerInvariant(), out var func)
                    ? argument => temp.Add(func(argument))
                    : argument => temp.Add(Convert.ChangeType(argument, element));

                foreach (var value in stringValues)
                {
                    action(value);
                }

                // proceeding with the actual instance of parameter's type
                // depending on what it is array or enumerable we proceed differently.
                if (isArray)
                {
                    var elementType = returnType.GetElementType();
                    resolvedParameter = Array.CreateInstance(elementType, stringValues.Count);
                    var array = resolvedParameter as Array;

                    for (var i = 0; i < temp.Count; i++)
                    {
                        array.SetValue(temp[i], i);
                    }
                }
                else
                {
                    // as we cannot create interface instance, we check if the return type is Interface member
                    // if true - we create list with generic argument of element type; otherwise - create instance of actual type
                    resolvedParameter = returnType.IsInterface ? Activator.CreateInstance(_listType.MakeGenericType(element)) : Activator.CreateInstance(returnType)!;
                    var list = resolvedParameter as IList;

                    foreach (var obj in temp)
                    {
                        list.Add(obj);
                    }
                }
            }
            // in case of negative verification we are trying to insert a default value
            else
            {
                resolvedParameter = parameter.HasDefaultValue ? parameter.DefaultValue : null;
            }

            // finally adding the actual instance into the sorted array
            lock (lockObject)
            {
                sortedInstances[index] = resolvedParameter;
            }
        }

        return Task.CompletedTask;
    }

    private record ResolvedParameter(object? Instance, int Index)
    {
        internal static readonly ResolvedParameter _empty = new(null, -1);

        internal static ResolvedParameter GetDefaultValue(ParameterInfo parameterInfo, int index) =>
            new(parameterInfo.HasDefaultValue ? parameterInfo.DefaultValue : null, index);
    }

    private record struct RouteParameter(string Value, Func<string, object>? Resolver);
}
