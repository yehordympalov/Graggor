using System.Reflection;
using AttributeApi.Services.Builders;
using AttributeApi.Services.Core;
using AttributeApi.Services.Parameters.Binders.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AttributeApi.Services.Parameters.Binders.Core;

internal class DefaultFromRouteParametersBinder(IOptions<AttributeApiRouteOptions> routeOptions) : IFromRouteParametersBinder
{
    private readonly AttributeApiRouteOptions _routeOptions = routeOptions.Value;

    public Task<IEnumerable<BindParameter>> BindParametersAsync(List<ParameterInfo> parameters, RouteParameter routeParameter)
    {
        var routeTemplate = routeParameter.RoutePattern;
        var requestPath = routeParameter.RequestPath;
        var routeNames = new List<string>();

        foreach (var parameter in parameters)
        {
            var attribute = parameter.GetCustomAttribute<FromRouteAttribute>();

            if (attribute is null)
            {
                continue;
            }

            routeNames.Add(attribute.Name ?? parameter.Name!);
        }

        if (routeNames.Count == 0 || !routeTemplate.Contains("{"))
        {
            return Task.FromResult(Enumerable.Empty<BindParameter>());
        }

        var routeSegments = routeTemplate.Trim('/').Split('/');
        var pathSegments = requestPath.Trim('/').Split('/');

        if (routeSegments.Length != pathSegments.Length)
        {
            throw new InvalidOperationException("Route and request path do not match.");
        }

        var list = new List<BindParameter>(routeNames.Count);

        for (var i = 0; i < routeSegments.Length; i++)
        {
            var routeSegment = routeSegments[i];

            if (routeSegment.StartsWith("{") && routeSegment.EndsWith("}"))
            {
                var split = routeSegment.Trim('{', '}').Split(':');

                if (split.Length == 2)
                {
                    var routeSegmentName = split[0];
                    var parameterTypeString = split[1];
                    var parameterName = routeNames.First(name => name.Equals(routeSegmentName));

                    if (_routeOptions.TypeResolver.TryGetValue(parameterTypeString, out var resolver))
                    {
                        var instance = resolver.Invoke(pathSegments[i]);
                        list.Add(new(parameterName, instance));
                    }
                    else
                    {
                        var parameter = parameters.First(parameter =>
                        {
                            var name = parameter.GetCustomAttribute<FromRouteAttribute>()!.Name ?? parameter.Name;

                            return name.Equals(parameterName);
                        });

                        list.Add(new(parameter.Name, parameter.HasDefaultValue ? parameter.DefaultValue : null));
                    }
                }
                else if (split.Length > 2)
                {
                    throw new InvalidOperationException($"Route pattern cannot have more than 1 related types. Please verify attributes for your endpoint {routeTemplate}");
                }
            }
        }

        return Task.FromResult<IEnumerable<BindParameter>>(list);
    }

    Task<IEnumerable<BindParameter>> IParametersBinder.BindParametersAsync(List<ParameterInfo> parameters,
        object requestObject) => BindParametersAsync(parameters, (RouteParameter)requestObject);
}
