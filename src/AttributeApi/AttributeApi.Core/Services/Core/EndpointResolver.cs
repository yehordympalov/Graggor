using AttributeApi.Core.Attributes;
using AttributeApi.Core.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace AttributeApi.Core.Services.Core;

internal class EndpointResolver(ParametersResolver parametersResolver, AttributeApiConfiguration configuration)
{
    private static readonly Type _resultType = typeof(IResult);
    private static readonly Type _taskType = typeof(Task<>);

    public Endpoint? CreateEndpoint(IService service, MethodInfo method, string serviceRoute)
    {
        var attribute = method.GetCustomAttribute<EndpointAttribute>()!;
        var route = configuration._url + serviceRoute + attribute.Route;
        var returnType = method.ReturnType;
        var isAsync = returnType.IsGenericType && returnType.GetGenericTypeDefinition() == _taskType;

        if (!(isAsync && _resultType.IsAssignableFrom(returnType.GenericTypeArguments[0])) && !_resultType.IsAssignableFrom(method.ReturnType))
        {
            return null;
        }

        return new Endpoint(RequestDelegate, new EndpointMetadataCollection(service), route);

        async Task RequestDelegate(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;

            if (!request.Method.Equals(attribute.HttpMethodType, StringComparison.CurrentCultureIgnoreCase))
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                await response.WriteAsync("Wrong request type.");

                return;
            }

            var query = request.Query.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.FirstOrDefault());
            var body = await new StreamReader(request.Body).ReadToEndAsync();
            var methodParameters = parametersResolver.ResolveParameters(method, configuration._options,
                new HttpContextData(route, request.PathBase + request.Path, body, query));
            var result = method.Invoke(service, methodParameters);

            if (isAsync)
            {
                result = await (dynamic)result!;
            }

            await ((IResult)result).ExecuteAsync(context);
        }
    }
}
