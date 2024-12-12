using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AttributeApi.Register;
using AttributeApi.Services.Interfaces;
using AttributeApi.Tests.InMemoryApi.Build;
using AttributeApi.Tests.InMemoryApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Tests;

public class RegistrationTests(WebFactory factory) : IClassFixture<WebFactory>
{
    private readonly IServiceCollection _services = new ServiceCollection();
    private readonly WebFactory _factory = factory;
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    [Fact]
    public void AddAttributeApi_WhenAssembliesIsPassed_ShouldAddConfiguration()
    {
        RegisterServices();
        var assembly = Assembly.GetAssembly(typeof(WebFactory));
        var types = assembly.GetTypes();
        var servicesInAssembly = types.Where(type => type.GetInterface(nameof(IService)) is not null).ToList();
        var servicesInDependencyInjection = _services.Where(service => service.ServiceType.IsAssignableFrom(typeof(IService))).ToList();

        Assert.True(servicesInAssembly.Count == servicesInDependencyInjection.Count);
    }
    private void RegisterServices()
    {
        _services.AddSingleton<IConfiguration, ConfigurationManager>();
        _services.AddAttributeApi(config =>
        {
            config.RegisterAssembly(Assembly.GetAssembly(typeof(TypedResultsService)));
        });
    }
}