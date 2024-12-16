using System.Reflection;
using System.Text.Json;
using AttributeApi.Services.Builders;
using AttributeApi.Services.Core;
using AttributeApi.Services.Parameters.Binders.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Services.Parameters.Binders.Core;

internal class DefaultFromHeadersParametersBinder([FromKeyedServices(AttributeApiConfiguration.OPTIONS_KEY)] JsonSerializerOptions options) : IFromHeadersParametersBinder
{
    public Task<IEnumerable<BindParameter>> BindParametersAsync(List<ParameterInfo> parameters, IHeaderDictionary headerDictionary)
    {
        var fromHeaderParameters = parameters.Where(parameter => parameter.GetCustomAttribute<FromHeaderAttribute>() is not null).ToList();

        var list = fromHeaderParameters.Select(parameter =>
        {
            var name = parameter.Name;
            var key = parameter.GetCustomAttribute<FromHeaderAttribute>()!.Name ?? name;
            object? resolvedParameter;

            if (headerDictionary.TryGetValue(key, out var value))
            {
                resolvedParameter = JsonSerializer.Deserialize(value, parameter.ParameterType, options);
            }
            else
            {
                resolvedParameter = parameter.HasDefaultValue ? parameter.DefaultValue : null;
            }

            return new BindParameter(name, resolvedParameter);
        });

        return Task.FromResult(list);
    }

    Task<IEnumerable<BindParameter>> IParametersBinder.BindParametersAsync(List<ParameterInfo> parameters,
        object requestObject) => BindParametersAsync(parameters, (IHeaderDictionary)requestObject);
}
