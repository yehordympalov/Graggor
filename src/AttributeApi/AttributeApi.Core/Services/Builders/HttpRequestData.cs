using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace AttributeApi.Services.Builders;

/// <summary>
/// Holder for income request and depended endpoint
/// </summary>
/// <param name="Parameters">Information about all parameters which are predicted in the target method of used endpoint</param>
/// <param name="RouteParameter">Instance which contains the route template of endpoint and the actual current request path</param>
/// <param name="Body">An instance of <see cref="Stream"/>
/// which contains all data which came in a body section of the current request</param>
/// <param name="QueryCollection">An instance of <see cref="Dictionary{TKey, TValue}"/>
/// which contains all data which came in a query section of the current request</param>
/// <param name="HeaderDictionary">An instance which contains all headers of the current request</param>
public record struct HttpRequestData(List<ParameterInfo> Parameters, RouteParameter RouteParameter,
    Stream Body, IQueryCollection QueryCollection, IHeaderDictionary HeaderDictionary);
