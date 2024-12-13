using System.Reflection;
using AttributeApi.Services.Builders;
using AttributeApi.Services.Parameters.Binders.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AttributeApi.Services.Parameters.Binders.Core;

internal class DefaultFromRoutesParametersBinder : IFromRoutesParametersesBinder
{
    public Task<IEnumerable<BindParameter>> BindParametersAsync(List<ParameterInfo> parameters, RouteParameter routeParameter)
    {
        var routeTemplate = routeParameter.RoutePattern;
        var requestPath = routeParameter.RequestPath;
        var list = new List<BindParameter>();
        var fromRouteParameters = parameters.Where(parameter => parameter.GetCustomAttribute<FromRouteAttribute>() is not null).ToList();

        if (fromRouteParameters.Count == 0 || !routeTemplate.Contains("{"))
        {
            return Task.FromResult<IEnumerable<BindParameter>>(list);
        }

        var routeSegments = routeTemplate.Trim('/').Split('/');
        var pathSegments = requestPath.Trim('/').Split('/');

        if (routeSegments.Length != pathSegments.Length)
        {
            throw new InvalidOperationException("Route and request path do not match.");
        }

        var routeParameters = new Dictionary<string, ResolverForParameter>();

        for (var i = 0; i < routeSegments.Length; i++)
        {
            if (routeSegments[i].StartsWith("{") && routeSegments[i].EndsWith("}"))
            {
                var split = routeSegments[i].Trim('{', '}').Split(':');
                var parameterName = split[0];

                if (split.Length == 2)
                {
                    var parameterTypeString = split[1];

                    DefaultParametersHandler._typeResolvers.TryGetValue(parameterTypeString, out var resolver);
                    routeParameters[parameterName] = new ResolverForParameter(pathSegments[i], resolver);
                }
                else if (split.Length > 2)
                {
                    throw new InvalidOperationException($"Route pattern cannot have more than 1 related types. Please verify attributes for your endpoint {routeTemplate}");
                }
            }
        }

        foreach (var parameter in fromRouteParameters)
        {
            var name = parameter.GetCustomAttribute<FromRouteAttribute>().Name ?? parameter.Name;

            if (routeParameters.TryGetValue(name, out var resolvedRouteParameter))
            {
                var resolvedParameter = resolvedRouteParameter.Resolver is not null
                    ? resolvedRouteParameter.Resolver.Invoke(resolvedRouteParameter.Value)
                    : Convert.ChangeType(resolvedRouteParameter.Value, parameter.ParameterType);

                list.Add(new(name, resolvedParameter));
            }
        }

        return Task.FromResult<IEnumerable<BindParameter>>(list);
    }

    Task<IEnumerable<BindParameter>> IParametersBinder.BindParametersAsync(List<ParameterInfo> parameters,
        object requestObject) => BindParametersAsync(parameters, (RouteParameter)requestObject);

    private record struct ResolverForParameter(string Value, Func<string, object>? Resolver);
}
