using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using AttributeApi.Exceptions;
using AttributeApi.Services.Core;
using AttributeApi.Services.Interfaces;
using AttributeApi.Services.Parameters.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AttributeApi.Services.Builders;

/// <summary>
/// Default request builder for related specific endpoint.
/// </summary>
/// <param name="handler"></param>
internal class DefaultEndpointRequestDelegateBuilder(IParametersHandler parametersHandler) : IEndpointRequestDelegateBuilder
{
    private readonly Type _taskType = typeof(Task);
    private readonly Type _valueTaskType = typeof(ValueTask);
    private readonly Type _voidType = typeof(void);

    public IParametersHandler ParametersHandler { get; } = parametersHandler;

    public RequestDelegate CreateRequestDelegate(object instance, MethodInfo method, string httpMethod, string routeTemplate)
    {
        return RequestDelegate;

        async Task RequestDelegate(HttpContext context)
        {
            var request = context.Request;
            var requestPath = request.PathBase + request.Path;
            var httpRequestData = new HttpRequestData(method.GetParameters().ToList(), routeTemplate, requestPath, request.Body, request.Query);
            var parametersTask = ParametersHandler.HandleParametersAsync(httpRequestData);

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
                await EndpointExecutor.ExecuteAsync(context, attributeApiException.Result, (JsonTypeInfo<object>)ParametersHandler.Options.GetTypeInfo(typeof(object)));

                return;
            }
            catch (Exception exception)
            {
                if (exception.InnerException is AttributeApiException attributeApiException)
                {
                    await EndpointExecutor.ExecuteAsync(context, attributeApiException.Result, (JsonTypeInfo<object>)ParametersHandler.Options.GetTypeInfo(typeof(object)));

                    return;
                }

                throw;
            }

            await EndpointExecutor.ExecuteAsync(context, result, (JsonTypeInfo<object>)ParametersHandler.Options.GetTypeInfo(typeof(object)));
        }
    }
}
