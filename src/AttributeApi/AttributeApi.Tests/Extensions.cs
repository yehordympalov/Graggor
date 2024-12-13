﻿using Microsoft.AspNetCore.TestHost;
using System.Text;
using System.Text.Json;
using AttributeApi.Services.Core;
using AttributeApi.Tests.InMemoryApi.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Tests;

public static class Extensions
{
    public static RequestBuilder BuildRequest(this TestServer server, string serviceRoute, object? content = null, string pattern = "")
    {
        var httpContent = new StringContent(string.Empty, Encoding.UTF8);

        if (content is not null)
        {
            var json = JsonSerializer.Serialize(content, server.Services.GetRequiredKeyedService<JsonSerializerOptions>(AttributeApiConfiguration.OPTIONS_KEY));
            httpContent = new StringContent(json, Encoding.UTF8);
        }

        var path = $"api/v1/{serviceRoute}";

        if (!string.IsNullOrWhiteSpace(pattern))
        {
            path = path + "/" + pattern;
        }

        return server.CreateRequest(path).And(config => config.Content = httpContent);
    }

    public static async Task<User> ExtractUserAsync(this HttpContent content, JsonSerializerOptions options)
    {
        var stream = await content.ReadAsStreamAsync();

        return await JsonSerializer.DeserializeAsync<User>(stream, options);
    }

    public static Task<HttpResponseMessage> PutAsync(this RequestBuilder request) => request.SendAsync(HttpMethod.Put.Method);

    public static Task<HttpResponseMessage> PatchAsync(this RequestBuilder request) => request.SendAsync(HttpMethod.Patch.Method);

    public static Task<HttpResponseMessage> DeleteAsync(this RequestBuilder request) => request.SendAsync(HttpMethod.Delete.Method);
}
