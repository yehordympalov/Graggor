using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AttributeApi.Exceptions;
using AttributeApi.Services.Core;
using Microsoft.AspNetCore.Http;

namespace AttributeApi.Services.Builders;

/// <summary>
/// Builder for the <see cref="RequestDelegate"/> to be called with specific endpoint
/// </summary>
internal static class EndpointRequestDelegateBuilder
{
    internal static JsonSerializerOptions _options;
    internal static Type _taskType = typeof(Task);
    internal static Type _valueTaskType = typeof(ValueTask);
    internal static Type _voidType = typeof(void);

    /// <summary>
    /// Method which returns the built <see cref="RequestDelegate"/> to be called with specific endpoint
    /// </summary>
    /// <param name="logger">Instance to log statement</param>
    /// <param name="instance">Instance of service which will be a target of method execution</param>
    /// <param name="method">All data of the method to be executed</param>
    /// <param name="httpMethod">HTTP Method type of the current endpoint</param>
    /// <param name="routeTemplate">Template of the current endpoint to be parsed with values if it's predicted</param>
    /// <returns>Instance of <see cref="RequestDelegate"/></returns>
    public static RequestDelegate CreateRequestDelegate(object instance, MethodInfo method, string httpMethod, string routeTemplate)
    {
        return RequestDelegate;

        async Task RequestDelegate(HttpContext context)
        {
            var request = context.Request;
            var requestPath = request.PathBase + request.Path;
            var query = request.Query.Count is not 0 ? request.Query.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.FirstOrDefault()) : [];
            var httpRequestData = new HttpRequestData(method.GetParameters().ToList(), routeTemplate, requestPath, request.Body, query);
            var parametersTask = ParametersBuilder.ResolveParametersAsync(httpRequestData);

            var returnType = method.ReturnType;
            var isReturnable = returnType != _voidType && returnType != _taskType && returnType.BaseType != _valueTaskType;
            var isTask = returnType == _taskType || returnType.BaseType == _taskType;
            var isValueTask = returnType == _valueTaskType || returnType.BaseType == _valueTaskType;
            var isAsync = isTask || isValueTask;
            object? result = null;

            var methodParameters = await parametersTask;

            try
            {
                if (isReturnable)
                {
                    if (isAsync)
                    {
                        result = await (dynamic)method.Invoke(instance, methodParameters);
                    }
                    else
                    {
                        result = method.Invoke(instance, methodParameters);
                    }
                }
                else
                {
                    if (isAsync)
                    {
                        if (isTask)
                        {
                            await (Task)method.Invoke(instance, methodParameters);
                        }
                        else
                        {
                            await (ValueTask)method.Invoke(instance, methodParameters);
                        }
                    }
                    else
                    {
                        method.Invoke(instance, methodParameters);
                    }
                }
            }
            catch (AttributeApiException attributeApiException)
            {
                await EndpointExecutor.ExecuteAsync(context, attributeApiException.Result, (JsonTypeInfo<object>)_options.GetTypeInfo(typeof(object)));

                return;
            }
            catch (Exception exception)
            {
                if (exception.InnerException is AttributeApiException attributeApiException)
                {
                    await EndpointExecutor.ExecuteAsync(context, attributeApiException.Result, (JsonTypeInfo<object>)_options.GetTypeInfo(typeof(object)));

                    return;
                }

                throw;
            }

            await EndpointExecutor.ExecuteAsync(context, result, (JsonTypeInfo<object>)_options.GetTypeInfo(typeof(object)));
        }
    }
}
