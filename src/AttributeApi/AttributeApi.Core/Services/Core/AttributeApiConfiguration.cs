using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace AttributeApi.Core.Services.Core;

public class AttributeApiConfiguration()
{
    internal List<Assembly> _assemblies = [];

    internal JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    internal string _url = string.Empty;

    public AttributeApiConfiguration RegisterAssembly(Assembly assembly)
    {
        if (!_assemblies.Contains(assembly))
        {
            _assemblies.Add(assembly);
        }

        return this;
    }

    public AttributeApiConfiguration AddJsonSerializerOptions(JsonSerializerOptions options)
    {
        _options = options;

        return this;
    }
}
