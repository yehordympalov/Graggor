using System.Reflection;
using AttributeApi.Register;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services
    .AddOpenApi()
    .AddLogging()
    .AddAttributeApi(configure => configure.RegisterAssembly(Assembly.GetExecutingAssembly()));

var app = builder.Build();

app.UseAttributeApiV2();
app.UseRouting();

await app.RunAsync();
