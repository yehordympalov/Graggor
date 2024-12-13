using Microsoft.Extensions.DependencyInjection;
using AttributeApi.Services.Core;
using AttributeApi.Services.Interfaces;
using AttributeApi.Attributes;
using System.Reflection;
using System.Text.Json;
using AttributeApi.Services.Parameters.Binders.Interfaces;
using AttributeApi.Services.Parameters.Interfaces;

namespace AttributeApi.Register;

public static class ServiceCollectionExtensions
{
    internal static readonly Type _serviceType = typeof(IService);

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

        configuration.Assemblies.ForEach(assembly =>
        {
            var allTypes = assembly.GetTypes();
            attributeServices.AddRange(allTypes.Where(type => type.GetCustomAttribute<ApiAttribute>() is not null  && type is { IsAbstract: false, IsInterface: false }));
        });

        services.AddHttpContextAccessor();
        services.AddSingleton(configuration);
        services.AddSingleton(typeof(IParametersBinder), configuration.ParameterBindersConfiguration.FromBodyParameterBinderType);
        services.AddSingleton(typeof(IParametersBinder), configuration.ParameterBindersConfiguration.FromHeadersBindersType);
        services.AddSingleton(typeof(IParametersBinder), configuration.ParameterBindersConfiguration.FromServicesParametersBinderType);
        services.AddSingleton(typeof(IParametersBinder), configuration.ParameterBindersConfiguration.FromKeyedServicesParametersBinderType);
        services.AddSingleton(typeof(IParametersBinder), configuration.ParameterBindersConfiguration.FromQueryParametersBindersType);
        services.AddSingleton(typeof(IParametersBinder), configuration.ParameterBindersConfiguration.FromRoutesParametersBindersType);
        services.AddSingleton(typeof(IParametersBinder), configuration.ParameterBindersConfiguration.AttributelessParametersBinderType);
        services.AddSingleton(typeof(IEndpointRequestDelegateBuilder), configuration.EndpointRequestDelegateBuilderType);
        services.AddSingleton(typeof(IParametersHandler), configuration.ParametersHandlerType);
        services.AddRange(attributeServices.Select(type => new ServiceDescriptor(type, type, configuration.ServicesLifetime)).ToList());
        services.Add(new ServiceDescriptor(typeof(JsonSerializerOptions), AttributeApiConfiguration.OPTIONS_KEY, configuration.Options));

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
