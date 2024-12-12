using System.Reflection;
using AttributeApi.Services.Builders;
using AttributeApi.Services.Parameters.Binders.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AttributeApi.Services.Parameters.Binders.Core;

internal class DefaultFromServiceParameterBinder : IFromServiceParameterBinder
{
    public Task<IEnumerable<BindParameter>> BindParametersAsync(List<ParameterInfo> parameters, IServiceProvider serviceProvider) => 
        Task.FromResult(parameters.Where(parameter => parameter.GetCustomAttribute<FromServicesAttribute>() is not null)
            .ToList().Select(parameter => new BindParameter(parameter.Name, serviceProvider.GetService(parameter.ParameterType))));
}
