using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AttributeApi.Core.Register;
using AttributeApi.Service.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Tests;

public class RegistrationTests : IClassFixture<WebFactory>
{
    private readonly IServiceCollection _services;
    private readonly HttpClient _httpClient;
    private readonly WebFactory _factory;
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public RegistrationTests(WebFactory factory)
    {
        _factory = factory;
        _services = new ServiceCollection();
        _httpClient = new HttpClient();
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "development", EnvironmentVariableTarget.Process);
    }

    [Fact]
    public void AddAttributeApi_WhenAssembliesIsPassed_ShouldAddConfiguration()
    {
        RegisterServices();
        var service = _services.FirstOrDefault(service => service.ImplementationType == typeof(UserService));

        Assert.NotNull(service);
    }

    [Fact]
    public async Task UseAttributeApi_WhenAssembliesArePassed_ShouldStartEndpoints()
    {
        var user = new User(Guid.CreateVersion7(), "userName", "123");
        var json = JsonSerializer.Serialize(user, _options);
        var result = await _factory.Server.CreateRequest("api/v1/users").And(config => config.Content = new StringContent(json, Encoding.UTF8)).PostAsync();

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    private void RegisterServices()
    {
        _services.AddSingleton<IConfiguration, ConfigurationManager>();
        _services.AddAttributeApi(config =>
        {
            config.AddJsonSerializerOptions(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            config.RegisterAssembly(Assembly.GetAssembly(typeof(UserService)));
        });
    }
}