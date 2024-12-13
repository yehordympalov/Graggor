using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AttributeApi.Exceptions;
using AttributeApi.Services.Core;
using AttributeApi.Services.Interfaces;
using AttributeApi.Services.Parameters.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Services.Builders;

/// <summary>
/// Default request builder for related specific endpoint.
/// </summary>
/// <param name="parametersHandler"></param>
internal class DefaultEndpointRequestDelegateBuilder(IServiceProvider serviceProvider,
    IParametersHandler parametersHandler) : IEndpointRequestDelegateBuilder
{
    private readonly Type _taskType = typeof(Task);
    private readonly Type _valueTaskType = typeof(ValueTask);
    private readonly Type _voidType = typeof(void);

    public IParametersHandler ParametersHandler { get; } = parametersHandler;

    public RequestDelegate CreateRequestDelegate(Type serviceType, MethodInfo method, string httpMethod, string routePattern)
    {
        return RequestDelegate;

        async Task RequestDelegate(HttpContext context)
        {
            using var scope = serviceProvider.CreateScope();
            var instance = scope.ServiceProvider.GetRequiredService(serviceType);
            var request = context.Request;
            var requestPath = request.PathBase + request.Path;
            var httpRequestData = new HttpRequestData(method.GetParameters().ToList(), new RouteParameter(routePattern, requestPath), request.Body, request.Query, request.Headers);
            var parametersTask = ParametersHandler.HandleParametersAsync(httpRequestData);

            // this verification is done to be sure of right method execution and value returning
            // cast to dynamic is heavy operation, that's why we use this verification, to save 
            // performance if it's possible. Also, Task and void types do not return any value
            // To prevent an exception we still verify if return type is not void or Task
            var returnType = method.ReturnType;
            var isReturnable = returnType != _voidType && returnType != _taskType && returnType.BaseType != _valueTaskType;
            var isTask = returnType == _taskType || returnType.BaseType == _taskType;
            var isValueTask = returnType == _valueTaskType || returnType.BaseType == _valueTaskType;
            var isAsync = isTask || isValueTask;
            var options = serviceProvider.GetRequiredKeyedService<JsonSerializerOptions>(AttributeApiConfiguration.OPTIONS_KEY);
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
                await EndpointExecutor.ExecuteAsync(context, attributeApiException.Result, (JsonTypeInfo<object>)options.GetTypeInfo(typeof(object)));

                return;
            }
            // if this exception is caused by AttributeApiException type
            // it can happen in some scenarios that the received exception is not type of AttributeApiException
            // we are executing the type result invoking to send the predicted response status code instead of (500)InternalServerError
            catch (Exception exception)
            {
                if (exception.InnerException is AttributeApiException attributeApiException)
                {
                    await EndpointExecutor.ExecuteAsync(context, attributeApiException.Result, (JsonTypeInfo<object>)options.GetTypeInfo(typeof(object)));

                    return;
                }

                throw;
            }

            await EndpointExecutor.ExecuteAsync(context, result, (JsonTypeInfo<object>)options.GetTypeInfo(typeof(object)));
        }
    }
}
