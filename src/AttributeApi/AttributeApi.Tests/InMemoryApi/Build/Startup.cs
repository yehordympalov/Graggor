using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AttributeApi.Register;
using AttributeApi.Services.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
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
        services.AddAttributeApi(configure =>
        {
            configure.RegisterAssembly(Assembly.GetExecutingAssembly());
            configure.AddJsonOptions(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            });
            configure.ServicesLifetime = ServiceLifetime.Scoped;
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseMiddleware<AttributeApiMiddleware>();
        app.UseHttpLogging();
        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.UseAttributeApiV2());
    }
}