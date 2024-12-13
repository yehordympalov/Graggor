using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace AttributeApi.Services.Builders;

/// <summary>
/// Holder for income request and depended endpoint
/// </summary>
/// <param name="Parameters">Information about all parameters which are predicted in the target method of used endpoint</param>
/// <param name="RouteTemplate">Template of the current endpoint to be parsed with values if it's predicted</param>
/// <param name="RequestPath">Full path of the current request</param>
/// <param name="Body">An instance of <see cref="Stream"/>
/// which contains all data which came in a body section of the current request</param>
/// <param name="Query">An instance of <see cref="Dictionary{TKey, TValue}"/>
/// which contains all data which came in a query section of the current request</param>
public record struct HttpRequestData(List<ParameterInfo> Parameters, RouteParameter RouteParameter,
    Stream Body, IQueryCollection QueryCollection, IHeaderDictionary HeaderDictionary);
