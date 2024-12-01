using System.Reflection;
using AttributeApi.Attributes;
using AttributeApi.Services.Core;
using AttributeApi.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Register;

public static class ServiceCollectionExtensions
{
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

        services.AddSingleton(configuration);
        services.AddSingleton(typeof(IApiHandler), configuration.ApiImplementationType);
        services.AddSingleton(typeof(IHandlerConfiguration), configuration.ApiConfigurationType);
        services.AddRange(configuration.Pipelines);
        var provider = services.BuildServiceProvider();
        var handlerConfiguration = provider.GetRequiredService<IHandlerConfiguration>();
        var activeEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        if (string.IsNullOrEmpty(activeEnvironment))
        {
            throw new InvalidOperationException("Cannot find .NET environment. Ensure you have environment variable DOTNET_ENVIRONMENT");
        }

        var webConfiguration = provider.GetRequiredService<IConfiguration>();
        var applicationUrl = webConfiguration[$"profiles:{activeEnvironment}:applicationUrl"];

        if (string.IsNullOrWhiteSpace(applicationUrl))
        {
            throw new InvalidOperationException("Application URL is not set on your active environment. Ensure you have applicationUrl in your json file");
        }

        configuration.Assemblies.ForEach(assembly =>
        {
            var endpointServices = assembly.GetTypes().Where(type => type.GetCustomAttribute<ApiAttribute>(false) is not null && type.GetBaseType(nameof(IService)) is not null)
                .Select(type => new ServiceDescriptor(typeof(IService), $"{applicationUrl}/{type.GetCustomAttribute<ApiAttribute>()!.Route}", type, ServiceLifetime.Scoped)).ToList();
            handlerConfiguration.Services.AddRange(endpointServices.Select(endpointService => new ServiceConfiguration(endpointService.ServiceKey!.ToString()!, endpointService.ImplementationType!)));
            endpointServices.AddRange(endpointServices);
        });

        return services;
    }

    private static Type? GetBaseType(this Type type, string name)
    {
        Type? typeToReturn = null;
        var tempType = type;

        while (typeToReturn is null && tempType is not null)
        {
            tempType = tempType.BaseType;

            if (tempType is not null && tempType.Name.Equals(name))
            {
                typeToReturn = tempType;
            }
        }

        return typeToReturn;
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
