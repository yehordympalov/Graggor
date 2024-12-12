using System.Reflection;

namespace AttributeApi.Services.Builders;

public record BindParameter(string Name, object? Instance)
{
    public static BindParameter Empty { get; } = new(string.Empty, null);

    public static BindParameter WithDefaultValue(ParameterInfo info) => 
        new(info.Name, info.HasDefaultValue ? info.DefaultValue : null);
}
