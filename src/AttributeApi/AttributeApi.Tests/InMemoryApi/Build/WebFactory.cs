using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace AttributeApi.Tests.InMemoryApi.Build;

public class WebFactory : WebApplicationFactory<Startup>
{
    protected override IHostBuilder? CreateHostBuilder() => Host.CreateDefaultBuilder();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("development");
        builder.UseContentRoot(Directory.GetCurrentDirectory());
        builder.UseStartup<Startup>();
    }
}
