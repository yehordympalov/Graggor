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
        var fromServiceBinder = binders.Single(binder => binder.GetType().IsAssignableTo(typeof(IFromServicesParametersBinder)));
        var fromKeyedServiceBinder = binders.Single(binder => binder.GetType().IsAssignableTo(typeof(IFromKeyedServicesParametersBinder)));
        var fromHeadersBinder = binders.Single(binder => binder.GetType().IsAssignableTo(typeof(IFromHeadersParametersBinder)));
        var fromQueryBinder = binders.Single(binder => binder.GetType().IsAssignableTo(typeof(IFromQueryParametersBinder)));
        var fromRouteBinder = binders.Single(binder => binder.GetType().IsAssignableTo(typeof(IFromRouteParametersBinder)));
        var attributelessBinder = binders.Single(binder => binder.GetType().IsAssignableTo(typeof(IAttributelessParametersBinder)));

        var bindAttributelessTask = attributelessBinder.BindParametersAsync(parameters, serviceProvider);
        var bindFromServiceTask = fromServiceBinder.BindParametersAsync(parameters, serviceProvider);
        var bindFromKeyedServiceTask = fromKeyedServiceBinder.BindParametersAsync(parameters, serviceProvider);
        var bindFromBodyTask = fromBodyBinder.BindParametersAsync(parameters, data.Body);
        var bindFromHeadersTask = fromHeadersBinder.BindParametersAsync(parameters, data.HeaderDictionary);
        var bindFromQueryTask = fromQueryBinder.BindParametersAsync(parameters, data.QueryCollection);
        var bindFromRouteTask = fromRouteBinder.BindParametersAsync(parameters, data.RouteParameter);

        await Task.WhenAll(bindFromRouteTask, bindFromBodyTask, bindFromKeyedServiceTask,
            bindFromServiceTask, bindFromHeadersTask, bindFromQueryTask);


        parameters.ForEach(parameter =>
        {
            var attribute = parameter.GetCustomAttributes().First(attribute =>
            {
                var type = attribute.GetType();

                return type.IsAssignableTo(typeof(IBindingSourceMetadata)) || type == typeof(FromKeyedServicesAttribute);
            });
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
