using AttributeApi.Core.Attributes;
using AttributeApi.Core.Services.Core;
using AttributeApi.Core.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AttributeApi.Core.Register;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder UseAttributeApiV2(this IEndpointRouteBuilder app)
    {
        var serviceProvider = app.ServiceProvider;
        var services = serviceProvider.GetServices(ServiceCollectionExtensions._serviceType).ToList();

        if (services.Count is 0)
        {
            return app;
        }

        services = services.Where(service => service is not null).ToList();

        var middlewares = serviceProvider.GetServices(ServiceCollectionExtensions._middlewareType).ToList();
        var applicationBuilder = (IApplicationBuilder)app;
        middlewares.ForEach(middleware => applicationBuilder.UseMiddleware(middleware!.GetType()));
        services.ForEach(sv =>
        {
            var service = (IService)sv!;
            var serviceType = service.GetType();
            var serviceRoute = serviceType.GetCustomAttribute<ApiAttribute>()!.Route;
            var endpoints = serviceType.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(method => method.GetCustomAttribute<EndpointAttribute>(true) is not null).ToList();
            var endpointResolver = new EndpointResolver(new ParametersResolver(serviceProvider), serviceProvider.GetRequiredService<AttributeApiConfiguration>());
            endpoints.ForEach(endpoint =>
            {
                var route = endpoint.GetCustomAttribute<EndpointAttribute>(true)!.Route;
                var mappedEndpoint = endpointResolver.CreateEndpoint(service, endpoint, string.Empty);

                if (mappedEndpoint is not null)
                {
                    app.Map(serviceRoute + route, mappedEndpoint.RequestDelegate!);
                }
            });
        });

        return app;
    }

    public static IEndpointRouteBuilder UseAttributeApi(this IEndpointRouteBuilder app)
    {
        var serviceProvider = app.ServiceProvider;
        var services = serviceProvider.GetServices(ServiceCollectionExtensions._serviceType).ToList();

        if (services.Count is 0)
        {
            return app;
        }

        var configuration = serviceProvider.GetRequiredService<AttributeApiConfiguration>();
        var middlewares = serviceProvider.GetServices(ServiceCollectionExtensions._middlewareType).ToList();

        if (app is not IApplicationBuilder applicationBuilder)
        {
            throw new ArgumentException(nameof(app));
        }

        middlewares.ForEach(middleware => applicationBuilder.UseMiddleware(middleware!.GetType()));
        var endpoints = new List<Endpoint>();
        var endpointResolver = new EndpointResolver(new ParametersResolver(serviceProvider), configuration);
        services.ForEach(sv =>
        {
            var service = (IService)sv!;
            var serviceType = service.GetType();
            var serviceRoute = serviceType.GetCustomAttribute<ApiAttribute>()!.Route;
            var endpointMethods = serviceType.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(method => method.GetCustomAttribute<EndpointAttribute>(true) is not null).ToList();

            endpointMethods.ForEach(method =>
            {
                var endpoint = endpointResolver.CreateEndpoint(service, method, serviceRoute);

                if (endpoint is not null)
                {
                    endpoints.Add(endpoint);
                }
            });
        });

        app.DataSources.Add(new DefaultEndpointDataSource(endpoints));

        applicationBuilder.Use(async (context, next) =>
        {
            var endpoint = context.GetEndpoint();

            if (endpoint is not null)
            {
                await endpoint.RequestDelegate.Invoke(context);
            }

            await next();
        });

        return app;
    }
}