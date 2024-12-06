namespace AttributeApi.Core.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public abstract class EndpointAttribute(string httpMethodType, string route) : Attribute
{
    public string HttpMethodType { get; } = httpMethodType;

    public string Route { get; } = route;
}
