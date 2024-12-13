using System.Reflection;
using AttributeApi.Services.Builders;

namespace AttributeApi.Services.Parameters.Binders.Interfaces;

public interface IParametersBinder
{
    public Task<IEnumerable<BindParameter>> BindParametersAsync(List<ParameterInfo> parameters, object requestObject);
}

public interface IParametersBinder<in T> : IParametersBinder
{
    /// <summary>
    /// Gets all needed information about parameters and binds them with support of <paramref name="requestObject"/>
    /// </summary>
    /// <param name="parameters"><see cref="List{T}"/> of parameters information which are related to mapped endpoint method</param>
    /// <param name="requestObject">It can be a type resolver or a some part of request data. It's depended on the implementation</param>
    /// <returns>Instance of <see cref="IEnumerable{T}"/> which holds all bind parameters
    /// or an empty collection if no matched binding type has been found</returns>
    public Task<IEnumerable<BindParameter>> BindParametersAsync(List<ParameterInfo> parameters, T requestObject);
}
