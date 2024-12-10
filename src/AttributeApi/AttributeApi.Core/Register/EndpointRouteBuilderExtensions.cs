using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AttributeApi.Attributes;
using AttributeApi.Services.Builders;
using AttributeApi.Services.Core;
using AttributeApi.Services.Interfaces;
using System.Text.Json;
using System.Text;
using System.Reflection;

namespace AttributeApi.Register;

public static class EndpointRouteBuilderExtensions
{
    private static readonly Type _loggerType = typeof(ILogger<>);

    public static IEndpointRouteBuilder UseAttributeApiV2(this IEndpointRouteBuilder app)
    {
        var serviceProvider = app.ServiceProvider;
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(nameof(UseAttributeApiV2));
        var services = serviceProvider.GetServices(ServiceCollectionExtensions._serviceType).ToList();

        if (services.Count is 0)
        {
            return app;
        }

        if (app is IApplicationBuilder applicationBuilder)
        {
            applicationBuilder.UseMiddleware<AttributeApiMiddleware>();
            var middlewares = serviceProvider.GetServices(ServiceCollectionExtensions._middlewareType).ToList();
            middlewares.ForEach(middleware => applicationBuilder.UseMiddleware(middleware!.GetType()));
        }
        else
        {
            logger.LogWarning("Your application instance is not inherited from {IApplicationBuilder}. Skipping all middlewares.", nameof(IApplicationBuilder));
        }

        InitializeInternalStaticFields(serviceProvider, serviceProvider.GetRequiredService<AttributeApiConfiguration>().Options);
        services = services.Where(service => service is not null).ToList();
        services.ForEach(sv =>
        {
            var service = (IService)sv!;
            var serviceType = service.GetType();
            var serviceRoute = serviceType.GetCustomAttribute<ApiAttribute>()!.Route;
            var endpoints = serviceType.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(method => method.GetCustomAttribute<EndpointAttribute>(true) is not null).ToList();
            endpoints.ForEach(endpoint =>
            {
                var attribute = endpoint.GetCustomAttribute<EndpointAttribute>(true)!;
                var routeTemplate = BuildRouteTemplate(serviceRoute, attribute.Route);
                var requestDelegate = EndpointRequestDelegateBuilder.CreateRequestDelegate(service, endpoint, attribute.HttpMethodType, routeTemplate);
                app.MapMethods(routeTemplate, [attribute.HttpMethodType], requestDelegate);
            });
        });

        return app;
    }

    private static void InitializeInternalStaticFields(IServiceProvider serviceProvider, JsonSerializerOptions options)
    {
        EndpointRequestDelegateBuilder._options = options;
        ParametersBuilder._options = options;
        ParametersBuilder._serviceProvider = serviceProvider;
    }

    private static string BuildRouteTemplate(string serviceRoute, string endpointRoute)
    {
        var builder = new StringBuilder();

        if (!serviceRoute.StartsWith('/'))
        {
            builder.Append('/');
        }

        builder.Append(serviceRoute);

        if (!serviceRoute.EndsWith('/') && !endpointRoute.StartsWith('/'))
        {
            builder.Append('/');
        }

        builder.Append(endpointRoute);

        return builder.ToString();
    }

    //public static IEndpointRouteBuilder UseAttributeApi(this IEndpointRouteBuilder app)
    //{
    //    var serviceProvider = app.ServiceProvider;
    //    var services = serviceProvider.GetServices(ServiceCollectionExtensions._serviceType).ToList();

    //    if (services.Count is 0)
    //    {
    //        return app;
    //    }

    //    var configuration = serviceProvider.GetRequiredService<AttributeApiConfiguration>();
    //    var middlewares = serviceProvider.GetServices(ServiceCollectionExtensions._middlewareType).ToList();

    //    if (app is not IApplicationBuilder applicationBuilder)
    //    {
    //        throw new ArgumentException(nameof(app));
    //    }

    //    var routeEndpointDataSourceType = Assembly.Load("Microsoft.AspNetCore.Routing").GetType("Microsoft.AspNetCore.Routing.RouteEndpointDataSource")!;
    //    var addRequestDelegate = routeEndpointDataSourceType.GetMethod("AddRequestDelegate", BindingFlags.Instance | BindingFlags.Public)!;
    //    var routeEndpointDataSourceConstructor = routeEndpointDataSourceType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
    //        CallingConventions.VarArgs, [typeof(IServiceProvider), typeof(bool)], null)!;
    //    var routeEndpointDataSource = (EndpointDataSource)routeEndpointDataSourceConstructor.Invoke([serviceProvider, true]);

    //    middlewares.ForEach(middleware => applicationBuilder.UseMiddleware(middleware!.GetType()));
    //    var endpointResolver = new EndpointResolver(new ParametersResolver(serviceProvider), configuration._options);

    //    services.ForEach(sv =>
    //    {
    //        var service = (IService)sv!;
    //        var serviceType = service.GetType();
    //        var serviceRoute = serviceType.GetCustomAttribute<ApiAttribute>()!.Route;
    //        var endpointMethods = serviceType.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(method => method.GetCustomAttribute<EndpointAttribute>(true) is not null).ToList();

    //        endpointMethods.ForEach(method =>
    //        {
    //            var endpoint = endpointResolver.CreateEndpoint(service, method, serviceRoute);

    //            if (endpoint is not null)
    //            {
    //                static RequestDelegateResult CreateHandlerRequestDelegate(Delegate handler, RequestDelegateFactoryOptions options, RequestDelegateMetadataResult? metadataResult)
    //                {
    //                    var requestDelegate = (RequestDelegate)handler;

    //                    // Create request delegate that calls filter pipeline.
    //                    if (options.EndpointBuilder?.FilterFactories.Count > 0)
    //                    {
    //                        requestDelegate = RequestDelegateFilterPipelineBuilder.Create(requestDelegate, options);
    //                    }

    //                    IReadOnlyList<object> metadata = options.EndpointBuilder?.Metadata is not null ?
    //                        new List<object>(options.EndpointBuilder.Metadata) :
    //                        Array.Empty<object>();

    //                    return new RequestDelegateResult(requestDelegate, metadata);
    //                }

    //                addRequestDelegate.Invoke()
    //            }
    //        });
    //    });

    //    app.DataSources.Add(routeEndpointDataSource);

    //    applicationBuilder.Use(async (context, next) =>
    //    {
    //        var endpoint = context.GetEndpoint();

    //        if (endpoint is not null)
    //        {
    //            await endpoint.RequestDelegate!.Invoke(context);
    //        }

    //        await next();
    //    });

    //    return app;
    //}
}