using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Core.Services.Core;

public class AttributeApiConfiguration()
{
    internal List<Assembly> Assemblies { get; }= [];

    public JsonSerializerOptions Options { get; set; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public ServiceLifetime ServicesLifetime { get; set; } = ServiceLifetime.Singleton;

    public ServiceLifetime MiddlewaresLifetime{ get; set; } = ServiceLifetime.Singleton;

    public AttributeApiConfiguration RegisterAssembly(Assembly assembly)
    {
        if (!Assemblies.Contains(assembly))
        {
            Assemblies.Add(assembly);
        }

        return this;
    }
}
