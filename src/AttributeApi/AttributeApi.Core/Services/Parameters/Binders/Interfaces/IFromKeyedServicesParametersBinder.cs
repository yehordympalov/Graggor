using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Services.Parameters.Binders.Interfaces;

public interface IFromKeyedServicesParametersBinder : IParametersBinder<IKeyedServiceProvider>;
