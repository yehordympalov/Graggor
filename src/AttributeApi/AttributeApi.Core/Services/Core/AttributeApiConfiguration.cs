using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AttributeApi.Attributes;
using AttributeApi.Services.Builders;
using AttributeApi.Services.Interfaces;
using AttributeApi.Services.Parameters;
using AttributeApi.Services.Parameters.Binders;
using AttributeApi.Services.Parameters.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Services.Core;

public class AttributeApiConfiguration()
{
    private readonly Type _middlewareType = typeof(IMiddleware);

    public const string OPTIONS_KEY = "AttributeApiJsonSerializableOptionsKey";

    internal List<Assembly> Assemblies { get; } = [];

    internal Type EndpointRequestDelegateBuilderType { get; private set; } = typeof(DefaultEndpointRequestDelegateBuilder);

    internal Type ParametersHandlerType { get; private set; } = typeof(DefaultParametersHandler);

    internal ParameterBindersConfiguration ParameterBindersConfiguration { get; private set; } = new();

    internal List<Type> ServiceTypes => Assemblies.SelectMany(assembly => assembly.GetTypes().Where(type => type.GetCustomAttribute<ApiAttribute>() is not null && type is { IsAbstract: false, IsInterface: false })).ToList();

    internal List<Type> Middlewares => Assemblies.SelectMany(assembly => assembly.GetTypes().Where(type => _middlewareType.IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false })).ToList();

    internal JsonSerializerOptions Options { get; private set; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        AllowOutOfOrderMetadataProperties = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    public ServiceLifetime ServicesLifetime { get; set; } = ServiceLifetime.Singleton;

    public AttributeApiConfiguration RegisterAssembly(Assembly assembly)
    {
        if (!Assemblies.Contains(assembly))
        {
            Assemblies.Add(assembly);
        }

        return this;
    }

    public AttributeApiConfiguration AddJsonOptions(Action<JsonSerializerOptions> configure)
    {
        configure(Options);

        return this;
    }

    public AttributeApiConfiguration AddJsonOptions(JsonSerializerOptions options)
    {
        Options = options;

        return this;
    }

    //public AttributeApiConfiguration AddEndpointRequestDelegateBuilder<T>() where T : IEndpointRequestDelegateBuilder
    //{
    //    EndpointRequestDelegateBuilderType = typeof(T);

    //    return this;
    //}

    public AttributeApiConfiguration AddParametersHandler<T>() where T : IParametersHandler
    {
        ParametersHandlerType = typeof(T);

        return this;
    }

    public AttributeApiConfiguration AddParametersBinder(Action<ParameterBindersConfiguration> configure)
    {
        configure(ParameterBindersConfiguration);

        return this;
    }
}
