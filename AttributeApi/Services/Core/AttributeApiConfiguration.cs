using System.Reflection;
using System.Text.Json;

namespace AttributeApi.Services.Core;

public class AttributeApiConfiguration()
{
    internal List<Assembly> _assemblies = [];

    internal JsonSerializerOptions _options = new();

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
