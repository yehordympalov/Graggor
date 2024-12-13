using System.Reflection;
using AttributeApi.Services.Builders;
using AttributeApi.Services.Parameters.Binders.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Services.Parameters.Binders.Core;

internal class DefaultFromKeyedServicesParametersesBinder() : IFromKeyedServicesParametersesBinder
{
    public Task<IEnumerable<BindParameter>> BindParametersAsync(List<ParameterInfo> parameters, IServiceProvider serviceProvider)
    {
        var fromKeyedParameters = parameters.Where(parameter => parameter.GetCustomAttribute<FromKeyedServicesAttribute>() is not null).ToList();

        var list = new List<BindParameter>();

        fromKeyedParameters.ForEach(parameter =>
        {
            var key = parameter.GetCustomAttribute<FromKeyedServicesAttribute>().Key;
            list.Add(new BindParameter(parameter.Name, serviceProvider.GetRequiredKeyedService(parameter.ParameterType, key)));
        });

        return Task.FromResult<IEnumerable<BindParameter>>(list);
    }

    Task<IEnumerable<BindParameter>> IParametersBinder.BindParametersAsync(List<ParameterInfo> parameters,
        object requestObject) => BindParametersAsync(parameters, (IServiceProvider)requestObject);
}
