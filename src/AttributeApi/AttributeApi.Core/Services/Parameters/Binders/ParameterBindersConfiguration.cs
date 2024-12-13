using AttributeApi.Services.Parameters.Binders.Core;

namespace AttributeApi.Services.Parameters.Binders;

public class ParameterBindersConfiguration
{
    public Type FromBodyParameterBinderType { get; set; } = typeof(DefaultFromBodyParametersBinder);

    public Type FromQueryParametersBindersType { get; set; } = typeof(DefaultFromQueryParametersesBinder);

    public Type FromHeadersBindersType { get; set; } = typeof(DefaultFromHeadersParametersesBinder);

    public Type FromRoutesParametersBindersType { get; set; } = typeof(DefaultFromRoutesParametersBinder);

    public Type FromServicesParametersBinderType { get; set; } = typeof(DefaultFromServicesParametersesBinder);

    public Type FromKeyedServicesParametersBinderType { get; set; } = typeof(DefaultFromKeyedServicesParametersesBinder);

    internal Type AttributelessParametersBinderType { get; set; } = typeof(DefaultAttributelessParametersesBinder);
}
