using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AttributeApi.Register;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AttributeApi.Tests.InMemoryApi.Build;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddOpenApi();
        services.AddLogging();
        services.AddHttpLogging();
        services.AddAuthentication();
        services.AddAuthorization();
        services.AddMvc();
        services.AddAttributeApi(configure => configure.RegisterAssembly(Assembly.GetExecutingAssembly()));
        services.AddSingleton(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpLogging();
        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.UseAttributeApiV2());
        app.UseHttpsRedirection();
    }
}