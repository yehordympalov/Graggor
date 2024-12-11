using Microsoft.AspNetCore.TestHost;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Tests;

public static class Extensions
{
    public static RequestBuilder BuildRequest(this TestServer server, string serviceRoute, object? content = null, string pattern = "")
    {
        var httpContent = new StringContent(string.Empty, Encoding.UTF8);

        if (content is not null)
        {
            var json = JsonSerializer.Serialize(content, server.Services.GetRequiredService<JsonSerializerOptions>());
            httpContent = new StringContent(json, Encoding.UTF8);
        }

        var path = $"api/v1/{serviceRoute}";

        if (!string.IsNullOrWhiteSpace(pattern))
        {
            path = path + "/" + pattern;
        }

        return server.CreateRequest(path).And(config => config.Content = httpContent);
    }

    public static Task<HttpResponseMessage> PutAsync(this RequestBuilder request) => request.SendAsync(HttpMethod.Put.Method);

    public static Task<HttpResponseMessage> PatchAsync(this RequestBuilder request) => request.SendAsync(HttpMethod.Patch.Method);

    public static Task<HttpResponseMessage> DeleteAsync(this RequestBuilder request) => request.SendAsync(HttpMethod.Delete.Method);
}
