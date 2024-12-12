using System.Reflection;
using AttributeApi.Services.Builders;

namespace AttributeApi.Services.Parameters.Binders.Interfaces;

public interface IParameterBinder
{
    public Task<IEnumerable<BindParameter>> BindParametersAsync(List<ParameterInfo> parameters, object requestObject);
}

public interface IParameterBinder<in T>
{
    public Task<IEnumerable<BindParameter>> BindParametersAsync(List<ParameterInfo> parameters, T requestObject);
}
