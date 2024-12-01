using System.Reflection;
using AttributeApi.Services.Core;
using AttributeApi.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Register;

public class AttributeApiConfiguration()
{
    internal List<Assembly> Assemblies { get; } = [];

    internal List<ServiceDescriptor> Pipelines { get; } = [];
    
    public Type ApiImplementationType { get; set; } = typeof(ApiHandler);

    public Type ApiConfigurationType { get; set; } = typeof(HandlerConfiguration);

    public AttributeApiConfiguration RegisterAssembly(Assembly assembly)
    {
        if (!Assemblies.Contains(assembly))
        {
            Assemblies.Add(assembly);
        }

        return this;
    }

    public AttributeApiConfiguration RegisterAssemblyContainingType<T>() => RegisterAssemblyContainingType(typeof(T));

    public AttributeApiConfiguration RegisterAssemblyContainingType(Type type)
    {
        var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetType(type.FullName!) is not null);

        if (assembly is not null && !Assemblies.Contains(assembly))
        {
            Assemblies.Add(assembly);
        }

        return this;
    }

    public AttributeApiConfiguration RegisterPipeline(Type pipelineType, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ThrowIfPipelineTypeIsNotValid(pipelineType);

        return Add(typeof(IPipeline), pipelineType, lifetime);
    }

    public AttributeApiConfiguration RegisterPipeline(Type serviceType, Type implementationType, ServiceLifetime lifetime)
    {
        ThrowIfPipelineTypeIsNotValid(implementationType);

        return Add(serviceType, implementationType, lifetime);
    }

    private AttributeApiConfiguration Add(Type serviceType, Type implementationType, ServiceLifetime lifetime)
    {
        Pipelines.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));

        return this;
    }

    private static void ThrowIfPipelineTypeIsNotValid(Type pipelineType)
    {
        if (!pipelineType.IsGenericType)
        {
            throw new InvalidOperationException($"Type {pipelineType.Name} has to be generic to be registered as a pipeline.");
        }

        var interfaces = pipelineType.GetInterfaces().Where(i => i.IsGenericType).Select(i => i.GetGenericTypeDefinition());
        var implementedGenericInterfaces = new HashSet<Type>(interfaces.Where(i => i == typeof(IPipeline)));

        if (implementedGenericInterfaces.Count is 0)
        {
            throw new InvalidOperationException($"Type {pipelineType.Name} has to implement {typeof(IPipeline).FullName}.");
        }
    }
}
