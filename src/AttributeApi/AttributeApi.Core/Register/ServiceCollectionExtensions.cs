using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using AttributeApi.Core.Services.Interfaces;
using AttributeApi.Core.Services.Core;
using AttributeApi.Core.Attributes;

namespace AttributeApi.Core.Register;

public static class ServiceCollectionExtensions
{
    internal static readonly Type _serviceType = typeof(IService);
    internal static readonly Type _middlewareType = typeof(IMiddleware);

    public static IServiceCollection AddAttributeApi(this IServiceCollection services, Action<AttributeApiConfiguration> config)
    {
        var configuration = new AttributeApiConfiguration();
        config.Invoke(configuration);

        return services.AddAttributeApi(configuration);
    }

    public static IServiceCollection AddAttributeApi(this IServiceCollection services, AttributeApiConfiguration configuration)
    {
        if (configuration.Assemblies.Count is 0)
        {
            throw new ArgumentException("No assemblies have been registered.");
        }

        var attributeServices = new List<Type>();
        var middlewares = new List<Type>();

        configuration.Assemblies.ForEach(assembly =>
        {
            var allTypes = assembly.GetTypes();
            attributeServices.AddRange(allTypes.Where(type => type.GetCustomAttribute<ApiAttribute>() is not null && _serviceType.IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false }));
            middlewares.AddRange(allTypes.Where(type => _middlewareType.IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false }));
        });

        var serviceDescriptors = attributeServices.Select(type => new ServiceDescriptor(_serviceType, type, configuration.ServicesLifetime)).ToList();
        serviceDescriptors.AddRange(middlewares.Select(type => new ServiceDescriptor(_middlewareType, type, configuration.MiddlewaresLifetime)));

        services.AddSingleton(configuration);
        services.AddRange(serviceDescriptors);
        services.AddHttpContextAccessor();

        return services;
    }

    internal static IServiceCollection AddRange(this IServiceCollection services, IEnumerable<ServiceDescriptor> values)
    {
        foreach (var value in values)
        {
            services.Add(value);
        }

        return services;
    }
}
