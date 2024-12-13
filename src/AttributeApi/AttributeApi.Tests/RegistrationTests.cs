using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using AttributeApi.Attributes;
using AttributeApi.Register;
using AttributeApi.Services.Interfaces;
using AttributeApi.Tests.InMemoryApi.Build;
using AttributeApi.Tests.InMemoryApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Tests;

public class RegistrationTests
{
    private readonly IServiceCollection _services = new ServiceCollection();

    [Fact]
    public void AddAttributeApi_WhenAssembliesIsPassed_ShouldAddConfiguration()
    {
        RegisterServices();
        var assembly = Assembly.GetAssembly(typeof(WebFactory));
        var types = assembly.GetTypes();
        var servicesInAssembly = types.Where(type => type.GetCustomAttribute<ApiAttribute>() is not null).ToList();
        var servicesInDependencyInjection = _services.Where(service => service.ServiceType.GetCustomAttribute<ApiAttribute>() is not null).ToList();

        Assert.True(servicesInAssembly.Count == servicesInDependencyInjection.Count);
    }
    private void RegisterServices()
    {
        _services.AddSingleton<IConfiguration, ConfigurationManager>();
        _services.AddAttributeApi(config =>
        {
            config.RegisterAssembly(Assembly.GetAssembly(typeof(TypedResultsService)));
            config.AddJsonOptions(options =>
            {
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            });
        });
    }
}