using System.Reflection;

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
internal record HttpRequestData(List<ParameterInfo> Parameters, string RouteTemplate, string RequestPath, Stream Body, Dictionary<string, string?> Query);
