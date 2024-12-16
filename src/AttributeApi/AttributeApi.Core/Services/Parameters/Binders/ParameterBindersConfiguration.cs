using AttributeApi.Services.Parameters.Binders.Core;

namespace AttributeApi.Services.Parameters.Binders;

public class ParameterBindersConfiguration
{
    public Type FromBodyParameterBinderType { get; set; } = typeof(DefaultFromBodyParametersBinder);

    public Type FromQueryParametersBindersType { get; set; } = typeof(DefaultFromQueryParametersBinder);

    public Type FromHeadersBindersType { get; set; } = typeof(DefaultFromHeadersParametersBinder);

    public Type FromRoutesParametersBindersType { get; set; } = typeof(DefaultFromRouteParametersBinder);

    public Type FromServicesParametersBinderType { get; set; } = typeof(DefaultFromServicesParametersBinder);

    public Type FromKeyedServicesParametersBinderType { get; set; } = typeof(DefaultFromKeyedServicesParametersBinder);

    internal Type AttributelessParametersBinderType { get; set; } = typeof(DefaultAttributelessParametersBinder);
}
