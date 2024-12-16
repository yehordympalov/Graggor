using System.Reflection;
using AttributeApi.Services.Builders;
using AttributeApi.Services.Parameters.Binders.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Services.Parameters.Binders.Core;

internal class DefaultAttributelessParametersBinder : IAttributelessParametersBinder
{
    public Task<IEnumerable<BindParameter>> BindParametersAsync(List<ParameterInfo> parameters,
        IServiceProvider serviceProvider)
    {
        var list = parameters.Where(parameter => !parameter.GetCustomAttributes()
            .Any(attribute => attribute.GetType().IsAssignableTo(typeof(IBindingSourceMetadata)) && attribute.GetType() != typeof(FromKeyedServicesAttribute)))
            .Select(parameter => new BindParameter(parameter.Name, serviceProvider.GetService(parameter.ParameterType)));

        return Task.FromResult(list);
    }

    public Task<IEnumerable<BindParameter>> BindParametersAsync(List<ParameterInfo> parameters, object requestObject)
        => BindParametersAsync(parameters, (IServiceProvider)requestObject);
}
