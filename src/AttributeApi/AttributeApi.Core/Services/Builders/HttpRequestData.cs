namespace AttributeApi.Services.Builders;

internal record HttpRequestData(string RouteTemplate, string RequestPath, Stream Body, Dictionary<string, string?> Query);
