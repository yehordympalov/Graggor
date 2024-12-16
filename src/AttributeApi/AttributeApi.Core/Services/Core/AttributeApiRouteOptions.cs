namespace AttributeApi.Services.Core;

public class AttributeApiRouteOptions
{
    internal IDictionary<string, Func<string, object>> TypeResolver { get; } =
        new Dictionary<string, Func<string, object>>(StringComparer.OrdinalIgnoreCase)
        {
            { "string", body => body },
            { "guid", body => Guid.Parse(body) },
            { "int", body => Convert.ToInt32(body) },
            { "long", body => Convert.ToInt64(body) },
            { "int128", body => Int128.Parse(body) },
            { "float", body => Convert.ToSingle(body) },
            { "decimal", body => Convert.ToDecimal(body) },
            { "double", body => Convert.ToDouble(body) },
            { "datetime", body => DateTime.Parse(body) },
            { "timespan", body => TimeSpan.Parse(body) },
        };

    public T? ResolveType<T>(string routeType, string routeValue) => (T?)ResolveType(routeType, routeValue) ?? default;

    public object? ResolveType(string routeType, string routeValue) => TypeResolver.TryGetValue(routeType, out var func) ? func(routeValue) : null;
}
