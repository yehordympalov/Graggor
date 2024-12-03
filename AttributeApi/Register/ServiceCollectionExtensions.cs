using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AttributeApi.Attributes;
using AttributeApi.Services.Core;
using AttributeApi.Services.Interfaces;

namespace AttributeApi.Register;

public static class ServiceCollectionExtensions
{
    private static readonly Type _serviceType = typeof(IService);
    private static readonly Type _middlewareType = typeof(IMiddleware);
    private static readonly Type _resultType = typeof(IResult);
    private static readonly Type _taskType = typeof(Task);
    private static readonly ConcurrentDictionary<string, Func<HttpContext, Task>> _endpoints = new();

    public static IServiceCollection AddAttributeApi(this IServiceCollection services, Action<AttributeApiConfiguration> config)
    {
        var configuration = new AttributeApiConfiguration();
        config.Invoke(configuration);

        return services.AddAttributeApi(configuration);
    }

    public static IServiceCollection AddAttributeApi(this IServiceCollection services, AttributeApiConfiguration configuration)
    {
        if (configuration._assemblies.Count is 0)
        {
            throw new ArgumentException("No assemblies have been registered.");
        }

        var activeEnvironment = Environment.GetEnvironmentVariable("ASPNET_ENVIRONMENT");

        if (string.IsNullOrEmpty(activeEnvironment))
        {
            throw new ArgumentException("Cannot find .NET environment. Ensure you have environment variable ASPNET_ENVIRONMENT");
        }

        var webConfiguration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        configuration._url = webConfiguration[$"profiles:{activeEnvironment}:applicationUrl"]!;

        var attributeServices = new List<Type>();
        var middlewares = new List<Type>();

        configuration._assemblies.ForEach(assembly =>
        {
            var allTypes = assembly.GetTypes();
            attributeServices.AddRange(allTypes.Where(type => type.GetCustomAttribute<ApiAttribute>() is not null && _serviceType.IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false }));
            middlewares.AddRange(allTypes.Where(type => _middlewareType.IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false }));
        });

        var serviceDescriptors = attributeServices.Select(type => new ServiceDescriptor(_serviceType, type, ServiceLifetime.Singleton)).ToList();
        serviceDescriptors.AddRange(middlewares.Select(type => new ServiceDescriptor(_middlewareType, type, ServiceLifetime.Singleton)));

        services.AddSingleton(configuration);
        services.AddRange(serviceDescriptors);
        services.AddHttpContextAccessor();

        return services;
    }

    public static IApplicationBuilder UseAttributeApi(this IApplicationBuilder app)
    {
        var serviceProvider = app.ApplicationServices;

        var services = serviceProvider.GetServices(_serviceType).ToList();

        if (services.Count is 0)
        {
            return app;
        }

        var configuration = serviceProvider.GetRequiredService<AttributeApiConfiguration>();
        var middlewares = serviceProvider.GetServices(_middlewareType).ToList();
        var logger = serviceProvider.GetRequiredService<ILogger<IApplicationBuilder>>();
        middlewares.ForEach(middleware => app.UseMiddleware(middleware!.GetType()));
        services.ForEach(sv =>
        {
            var service = (IService)sv!;
            var serviceType = service.GetType();
            var serviceRoute = serviceType.GetCustomAttribute<ApiAttribute>()!.Route;
            var endpointMethods = serviceType.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(method => method.GetCustomAttribute<EndpointAttribute>(true) is not null).ToList();

            endpointMethods.ForEach(method =>
            {
                var attribute = method.GetCustomAttribute<EndpointAttribute>()!;
                var route = configuration._url + serviceRoute + attribute.Route;
                var returnType = method.ReturnType;
                var isAsync = returnType.IsGenericType && returnType.GetGenericTypeDefinition() == _taskType;

                if (!(isAsync && _resultType.IsAssignableFrom(returnType.GenericTypeArguments[0])) || !_resultType.IsAssignableFrom(method.ReturnType))
                {
                    return;
                }

                Func<HttpContext, Task> func = async context =>
                {
                    var request = context.Request;
                    var response = context.Response;

                    if (!request.Method.Equals(attribute.HttpMethodType, StringComparison.CurrentCultureIgnoreCase))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await response.WriteAsync("Wrong request type.");

                        return;
                    }

                    var query = context.Request.Query.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.FirstOrDefault());
                    var buffer = new byte[request.Body.Length];
                    await request.Body.ReadExactlyAsync(buffer);
                    var body = Encoding.UTF8.GetString(buffer);
                    var methodParameters = GetMethodParameters(serviceProvider, method, configuration._options, query, body, route, request.PathBase + request.Path);
                    var result = method.Invoke(service, methodParameters);

                    if (isAsync)
                    {
                        result = await (dynamic)result;
                    }

                    await response.WriteAsync(JsonSerializer.Serialize(result));
                };

                if (!_endpoints.TryAdd(route, func))
                {
                    logger.LogWarning("Cannot resolve issue for {Route} endpoint", route);
                }
            });
        });

        app.Use(async (context, next) =>
        {
            var timestamp = Stopwatch.GetTimestamp();
            var request = context.Request;
            var response = context.Response;
            var fullPath = request.PathBase + request.Path;
            logger.LogInformation("Entering {Route}", fullPath);

            if (_endpoints.TryGetValue(fullPath, out var handler))
            {
                try
                {
                    await handler(context);
                }
                catch (Exception exception)
                {
                    response.StatusCode = StatusCodes.Status500InternalServerError;
                    logger.LogError(new(1000), exception, exception.Message);
                    await response.WriteAsync(exception.Message);
                }
            }
            else
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                await response.WriteAsync($"Route {fullPath} is not found");
            }

            logger.LogInformation("{Route} executing has been finished in {ElapsedTime} ms.", fullPath, Stopwatch.GetElapsedTime(timestamp).Milliseconds);

            await next();
        });

        return app;
    }

    private static object[] GetMethodParameters(IServiceProvider serviceProvider, MethodInfo method, JsonSerializerOptions options, Dictionary<string, string?> query, string body, string route, string requestPath)
    {
        var parameters = method.GetParameters().ToList();
        var threadSafeList = new ConcurrentBag<ParameterInfo>(parameters);
        var lockObject = new Lock();
        var sortedParameters = new object[parameters.Count];

        Task.WaitAll(
            ProceedFromBodyParameter(ref lockObject, ref sortedParameters, options, threadSafeList, body),
            ProceedFromRouteParameters(ref lockObject, ref sortedParameters, threadSafeList, route, requestPath),
            ProceedFromServiceParameters(ref lockObject, ref sortedParameters, threadSafeList, serviceProvider),
            ProceedFromQueryRouteParameters(ref lockObject, ref sortedParameters, threadSafeList, query));

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

    private static Task ProceedFromServiceParameters(ref Lock lockObject, ref object[] array, ConcurrentBag<ParameterInfo> threadSafeList, IServiceProvider serviceProvider)
    {
        var fromServiceParameters = threadSafeList.Where(parameter => parameter.GetCustomAttribute<FromBodyAttribute>() is not null);

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

            if (query.TryGetValue(parameter.Name!, out var stringValue))
            {
                var value = Convert.ChangeType(stringValue, parameter.ParameterType)!;

                lock (lockObject)
                {
                    array[index] = value;
                }
            }
        }

        return Task.CompletedTask;
    }

    private static IServiceCollection AddRange(this IServiceCollection services, IEnumerable<ServiceDescriptor> values)
    {
        foreach (var value in values)
        {
            services.Add(value);
        }

        return services;
    }
}
