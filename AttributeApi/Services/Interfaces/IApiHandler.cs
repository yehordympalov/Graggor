namespace AttributeApi.Services.Interfaces;

public interface IApiHandler
{
    public Task StartListeningAsync(CancellationToken cancellationToken);

    public Task StopListeningAsync();
}
