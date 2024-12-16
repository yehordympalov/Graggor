using System.Reflection;
using AttributeApi.Services.Builders;
using AttributeApi.Services.Parameters.Binders.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Services.Parameters.Binders.Core;

internal class DefaultFromKeyedServicesParametersBinder() : IFromKeyedServicesParametersBinder
{
    public Task<IEnumerable<BindParameter>> BindParametersAsync(List<ParameterInfo> parameters, IKeyedServiceProvider serviceProvider)
    {
        var list = new List<BindParameter>(parameters.Count);

        foreach (var parameter in parameters)
        {
            var attribute = parameter.GetCustomAttribute<FromKeyedServicesAttribute>();

            if (attribute is null)
            {
                continue;
            }

            list.Add(new(parameter.Name, serviceProvider.GetKeyedService(parameter.ParameterType, attribute.Key)));
        }

        return Task.FromResult<IEnumerable<BindParameter>>(list);
    }

    Task<IEnumerable<BindParameter>> IParametersBinder.BindParametersAsync(List<ParameterInfo> parameters,
        object requestObject) => BindParametersAsync(parameters, (IKeyedServiceProvider)requestObject);
}
