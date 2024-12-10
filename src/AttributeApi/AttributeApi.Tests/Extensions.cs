using Microsoft.AspNetCore.TestHost;
using System.Text;
using System.Text.Json;
using AttributeApi.Tests.InMemoryApi.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Tests;

public static class Extensions
{
    public static async Task<T> DeserializeFromContentAsync<T>(this HttpContent content, JsonSerializerOptions options)
    {
        var stream = await content.ReadAsStreamAsync();

        return await JsonSerializer.DeserializeAsync<T>(stream, options);
    }

    public static RequestBuilder BuildRequest(this TestServer server, User? user = null, string pattern = "")
    {
        var content = new StringContent(string.Empty, Encoding.UTF8);

        if (user is not null)
        {
            var json = JsonSerializer.Serialize(user, server.Services.GetRequiredService<JsonSerializerOptions>());
            content = new StringContent(json, Encoding.UTF8);
        }

        var path = "api/v1/typedResults/users";

        if (!string.IsNullOrWhiteSpace(pattern))
        {
            path = path + "/" + pattern;
        }

        return server.CreateRequest(path).And(config => config.Content = content);
    }

    public static Task<HttpResponseMessage> PutAsync(this RequestBuilder request) => request.SendAsync(HttpMethod.Put.Method);

    public static Task<HttpResponseMessage> PatchAsync(this RequestBuilder request) => request.SendAsync(HttpMethod.Patch.Method);

    public static Task<HttpResponseMessage> DeleteAsync(this RequestBuilder request) => request.SendAsync(HttpMethod.Delete.Method);
}
