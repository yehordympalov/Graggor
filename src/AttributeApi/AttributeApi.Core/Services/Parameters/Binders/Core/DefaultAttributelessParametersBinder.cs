using System.Reflection;
using AttributeApi.Services.Builders;
using AttributeApi.Services.Parameters.Binders.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Services.Parameters.Binders.Core;

internal class DefaultAttributelessParametersesBinder : IAttributelessParametersesBinder
{
    public Task<IEnumerable<BindParameter>> BindParametersAsync(List<ParameterInfo> parameters,
        IServiceProvider serviceProvider)
    {
        var attributelessParameters = parameters.Where(parameter => !parameter.GetCustomAttributes()
            .Any(attribute => attribute.GetType().IsAssignableTo(typeof(IBindingSourceMetadata)) && attribute.GetType() != typeof(FromKeyedServicesAttribute))).ToList();
        var list = new List<BindParameter>(attributelessParameters.Count);
        attributelessParameters.ForEach(parameter => list.Add(new BindParameter(parameter.Name, serviceProvider.GetService(parameter.ParameterType))));

        return Task.FromResult<IEnumerable<BindParameter>>(list);
    }

    public Task<IEnumerable<BindParameter>> BindParametersAsync(List<ParameterInfo> parameters, object requestObject)
        => BindParametersAsync(parameters, (IServiceProvider)requestObject);
}
