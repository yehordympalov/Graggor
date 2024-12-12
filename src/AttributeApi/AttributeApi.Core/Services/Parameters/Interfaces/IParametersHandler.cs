using System.Text.Json;
using AttributeApi.Services.Builders;

namespace AttributeApi.Services.Parameters.Interfaces;

/// <summary>
/// Specific binder which resolves all parameters for chosen method of the endpoint
/// </summary>
public interface IParametersHandler
{
    /// <summary>
    /// Options for json serialization
    /// </summary>
    public JsonSerializerOptions Options { get; }

    /// <summary>
    /// Takes the all information about incoming request and endpoint's method
    /// and resolves all parameters for this method.
    /// </summary>
    /// <param name="data">Data to resolve parameters for the current request</param>
    /// <returns>Sorted array of <see cref="object"/> which contains all possible instances
    /// which are predicted as a target method parameters</returns>
    public Task<object?[]> HandleParametersAsync(HttpRequestData data);
}
