using System.Net;

namespace AttributeApi.Services.Interfaces;

public delegate Task RequestDelegate();

public interface IPipeline
{
    public Task HandleAsync(HttpListenerRequest request, RequestDelegate next, CancellationToken cancellationToken);
}
