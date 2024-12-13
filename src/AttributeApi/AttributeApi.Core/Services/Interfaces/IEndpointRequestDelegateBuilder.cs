using AttributeApi.Services.Parameters.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace AttributeApi.Services.Interfaces;

/// <summary>
/// Builder for the <see cref="RequestDelegate"/> to be called with specific endpoint
/// </summary>
public interface IEndpointRequestDelegateBuilder
{
    /// <summary>
    /// Binder for incoming from requests parameters.
    /// </summary>
    public IParametersHandler ParametersHandler { get; }

    /// <summary>
    /// Method which returns the built <see cref="RequestDelegate"/> to be called with specific endpoint
    /// </summary>
    /// <param name="serviceType">Type of service which will be a target of method execution</param>
    /// <param name="method">All data of the method to be executed</param>
    /// <param name="httpMethod">HTTP Method type of the current endpoint</param>
    /// <param name="routePattern">Template of the current endpoint to be parsed with values if it's predicted</param>
    /// <returns>Instance of <see cref="RequestDelegate"/></returns>
    public RequestDelegate CreateRequestDelegate(Type serviceType, MethodInfo method, string httpMethod, string routePattern);
}
