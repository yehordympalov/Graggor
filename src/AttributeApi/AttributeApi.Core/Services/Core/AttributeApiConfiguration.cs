using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AttributeApi.Services.Builders;
using AttributeApi.Services.Interfaces;
using AttributeApi.Services.Parameters;
using AttributeApi.Services.Parameters.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Services.Core;

public class AttributeApiConfiguration()
{
    internal List<Assembly> Assemblies { get; } = [];

    internal Type EndpointRequestDelegateBuilderType { get; private set; } = typeof(DefaultEndpointRequestDelegateBuilder);

    internal Type ParametersBinderType { get; private set; } = typeof(DefaultParametersHandler);

    public JsonSerializerOptions Options { get; set; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        AllowOutOfOrderMetadataProperties = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    public ServiceLifetime ServicesLifetime { get; set; } = ServiceLifetime.Singleton;

    public ServiceLifetime MiddlewaresLifetime { get; set; } = ServiceLifetime.Singleton;

    public AttributeApiConfiguration RegisterAssembly(Assembly assembly)
    {
        if (!Assemblies.Contains(assembly))
        {
            Assemblies.Add(assembly);
        }

        return this;
    }

    public AttributeApiConfiguration RegisterEndpointRequestDelegateBuilder<T>() where T : IEndpointRequestDelegateBuilder
    {
        EndpointRequestDelegateBuilderType = typeof(T);

        return this;
    }

    public AttributeApiConfiguration RegisterParametersBinder<T>() where T : IParametersHandler
    {
        ParametersBinderType = typeof(T);

        return this;
    }
}
