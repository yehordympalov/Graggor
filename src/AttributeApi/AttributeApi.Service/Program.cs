using System.Reflection;
using AttributeApi.Core.Register;

namespace AttributeApi.Service;
    
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var services = builder.Services;

        services
            .AddOpenApi()
            .AddLogging()
            .AddAttributeApi(configure => configure.RegisterAssembly(Assembly.GetExecutingAssembly()));

        var app = builder.Build();

        app.UseAttributeApiV2();
        app.UseHttpsRedirection();
        app.UseRouting();

        await app.RunAsync();
    }
}
