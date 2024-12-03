namespace AttributeApi.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public abstract class EndpointAttribute(string httpMethodType, string route): Attribute
{
    public string HttpMethodType { get; } = httpMethodType;

    public string Route { get; } = route;
}
