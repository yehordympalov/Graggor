using System.Text.Json;
using AttributeApi.Services.Core;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Extensions;

internal static class ServiceProviderExtensions
{
    public static JsonSerializerOptions GetJsonSerializerOptions(this IServiceProvider serviceProvider)
        => serviceProvider.GetRequiredKeyedService<JsonSerializerOptions>(AttributeApiConfiguration.OPTIONS_KEY);
}
