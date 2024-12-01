namespace AttributeApi.Attributes;

public sealed class ApiAttribute(string route) : Attribute
{
    public string Route { get; } = route;
}
