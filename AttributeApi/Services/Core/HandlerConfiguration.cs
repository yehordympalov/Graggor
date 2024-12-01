using AttributeApi.Services.Interfaces;

namespace AttributeApi.Services.Core;

internal class HandlerConfiguration: IHandlerConfiguration
{
    public List<ServiceConfiguration> Services { get; } = [];
}
