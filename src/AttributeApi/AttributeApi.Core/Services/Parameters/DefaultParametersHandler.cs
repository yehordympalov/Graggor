using System.Collections.Concurrent;
using System.Reflection;
using AttributeApi.Services.Builders;
using AttributeApi.Services.Parameters.Binders.Interfaces;
using AttributeApi.Services.Parameters.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Services.Parameters;

/// <summary>
/// Default parameters binder.
/// </summary>
internal class DefaultParametersHandler(IServiceProvider serviceProvider, IEnumerable<IParametersBinder> binders) : IParametersHandler
{
    /// <summary>
    /// Type resolver for route patterns
    /// </summary>
    internal static readonly ConcurrentDictionary<string, Func<string, object>> _typeResolvers = new();

    static DefaultParametersHandler()
    {
        _typeResolvers.TryAdd("guid", body => Guid.Parse(body));
        _typeResolvers.TryAdd("string", body => body);
        _typeResolvers.TryAdd("int", body => Convert.ToInt32(body));
        _typeResolvers.TryAdd("int64", body => Convert.ToInt64(body));
        _typeResolvers.TryAdd("int128", body => Int128.Parse(body));
        _typeResolvers.TryAdd("double", body => Convert.ToDouble(body));
        _typeResolvers.TryAdd("decimal", body => Convert.ToDecimal(body));
    }

    public async Task<object?[]> HandleParametersAsync(HttpRequestData data)
    {
        var parameters = data.Parameters;
        var count = parameters.Count;

        if (count is 0)
        {
            return [];
        }

        var sortedInstances = new object[count];
        var fromBodyBinder = binders.Single(binder => binder.GetType().IsAssignableTo(typeof(IFromBodyParametersBinder)));
        var fromServiceBinder = binders.Single(binder => binder.GetType().IsAssignableTo(typeof(IFromServicesParametersesBinder)));
        var fromKeyedServiceBinder = binders.First(binder => binder.GetType().IsAssignableTo(typeof(IFromKeyedServicesParametersesBinder)));
        var fromHeadersBinder = binders.Single(binder => binder.GetType().IsAssignableTo(typeof(IFromHeadersParametersesBinder)));
        var fromQueryBinder = binders.Single(binder => binder.GetType().IsAssignableTo(typeof(IFromQueryParametersesBinder)));
        var fromRouteBinder = binders.Single(binder => binder.GetType().IsAssignableTo(typeof(IFromRoutesParametersesBinder)));
        var attributelessBinder = binders.Single(binder => binder.GetType().IsAssignableTo(typeof(IAttributelessParametersesBinder)));

        var bindAttributelessTask = attributelessBinder.BindParametersAsync(parameters, serviceProvider);
        var bindFromServiceTask = fromServiceBinder.BindParametersAsync(parameters, serviceProvider);
        var bindFromBodyTask = fromBodyBinder.BindParametersAsync(parameters, data.Body);
        var bindFromKeyedServiceTask = fromKeyedServiceBinder.BindParametersAsync(parameters, serviceProvider);
        var bindFromHeadersTask = fromHeadersBinder.BindParametersAsync(parameters, data.HeaderDictionary);
        var bindFromQueryTask = fromQueryBinder.BindParametersAsync(parameters, data.QueryCollection);
        var bindFromRouteTask = fromRouteBinder.BindParametersAsync(parameters, data.RouteParameter);

        await Task.WhenAll(bindFromRouteTask, bindFromBodyTask, bindFromKeyedServiceTask,
            bindFromServiceTask, bindFromHeadersTask, bindFromQueryTask);

        parameters.ForEach(parameter =>
        {
            var attribute = parameter.GetCustomAttributes().First(attribute => attribute.GetType().IsAssignableTo(typeof(IBindingSourceMetadata)));
            var index = parameters.IndexOf(parameter);

            switch (attribute)
            {
                case FromBodyAttribute:
                    sortedInstances[index] = bindFromBodyTask.Result.First().Instance;
                    break;
                case FromServicesAttribute:
                    sortedInstances[index] = bindFromServiceTask.Result.First(result => result.Name.Equals(parameter.Name)).Instance;
                    break;
                case FromKeyedServicesAttribute:
                    sortedInstances[index] = bindFromServiceTask.Result.First(result => result.Name.Equals(parameter.Name)).Instance;
                    break;
                case FromHeaderAttribute headerAttribute:
                    var headerName = headerAttribute.Name ?? parameter.Name;
                    sortedInstances[index] = bindFromHeadersTask.Result.First(result => result.Name.Equals(headerName)).Instance;
                    break;
                case FromQueryAttribute queryAttribute:
                    var queryName = queryAttribute.Name ?? parameter.Name;
                    sortedInstances[index] = bindFromQueryTask.Result.First(result => result.Name.Equals(queryName)).Instance;
                    break;
                case FromRouteAttribute routeAttribute:
                    var routeName = routeAttribute.Name ?? parameter.Name;
                    sortedInstances[index] = bindFromRouteTask.Result.First(result => result.Name.Equals(routeName)).Instance;
                    break;
                default:
                    sortedInstances[index] = bindAttributelessTask.Result.First(result => result.Name.Equals(parameter.Name)).Instance;
                    break;
            }
        });

        return sortedInstances;
    }
}
