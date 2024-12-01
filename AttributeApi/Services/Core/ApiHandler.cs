using System.Net;
using System.Reflection;
using AttributeApi.Attributes;
using AttributeApi.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AttributeApi.Services.Core;

internal sealed class ApiHandler(ILogger<IApiHandler> logger, IHandlerConfiguration configuration, IServiceScopeFactory scopeFactory) : IApiHandler
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Type _attributeType = typeof(EndpointAttribute);

    private CancellationTokenSource? _linkedCancellationTokenSource = null;

    public Task StartListeningAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Attribute API start listening is called.");

        if (_linkedCancellationTokenSource is not null)
        {
            return Task.FromException(new InvalidOperationException("Attribute API is already running."));
        }

        _linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        configuration.Services.ForEach(serviceConfiguration =>
        {
            serviceConfiguration.ImplementationType!.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(method => method.GetCustomAttribute(_attributeType) is not null)
                .Select(method => new Endpoint(serviceConfiguration.ImplementationType, serviceConfiguration.ServiceKey!, method, method.GetCustomAttribute<EndpointAttribute>()!)).ToList()
                .ForEach(endpoint => StartEndpointAsync(endpoint, _cancellationTokenSource.Token));
        });

        while (!cancellationToken.IsCancellationRequested)
        {

        }

        _cancellationTokenSource.Cancel();

        logger.LogDebug("Attribute API cancel is called.");

        return Task.CompletedTask;
    }

    public Task StopListeningAsync() => _linkedCancellationTokenSource is null ? Task.FromException(new InvalidOperationException("Attribute API is not running.")) : _linkedCancellationTokenSource.CancelAsync();

    private async Task StartEndpointAsync(Endpoint endpoint, CancellationToken cancellationToken)
    {
        var httpListener = new HttpListener()
        {
            Prefixes = { $"{endpoint.ServiceKey}/{endpoint.Attribute.Route}"},
        };

        while (!cancellationToken.IsCancellationRequested)
        {
            httpListener.Start();
            var context = await httpListener.GetContextAsync().ConfigureAwait(false);
            ExecuteEndpointAsync(context, endpoint, cancellationToken);
        }
    }

    private async Task ExecuteEndpointAsync(HttpListenerContext context, Endpoint endpoint, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var service = serviceProvider.GetRequiredKeyedService<IService>(endpoint.ServiceKey);
        var pipelines = serviceProvider.GetServices(typeof(IPipeline)).Reverse().Aggregate((RequestDelegate));
    }

    private static void ReturnBadRequestIfNotValid()
    {

    }

    private record Endpoint(Type ServiceType, object ServiceKey, MethodInfo MethodInfo, EndpointAttribute Attribute);
}
