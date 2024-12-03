namespace AttributeApi.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ApiAttribute(string route) : Attribute
{
    public string Route { get; } = route;
}
