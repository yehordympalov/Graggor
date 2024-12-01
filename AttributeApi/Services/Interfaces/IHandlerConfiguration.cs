using AttributeApi.Services.Core;

namespace AttributeApi.Services.Interfaces;

public interface IHandlerConfiguration
{
    public List<ServiceConfiguration> Services { get; }
}