using System.Reflection;
using System.Text.Json;
using System.Text;
using AttributeApi.Services.Builders;
using AttributeApi.Services.Parameters.Binders.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AttributeApi.Services.Parameters.Binders.Core;

internal class DefaultFromBodyParameterBinder(JsonSerializerOptions options) : IFromBodyParameterBinder
{
    public async Task<IEnumerable<BindParameter>> BindParametersAsync(List<ParameterInfo> parameters, Stream body)
    {
        var parameter = parameters.SingleOrDefault(parameter => parameter.GetCustomAttribute<FromBodyAttribute>() is not null);
        var bind = new List<BindParameter>();

        if (parameter != null)
        {
            var index = parameters.IndexOf(parameter);
            object? resolvedParameter;

            if (body.CanSeek)
            {
                if (body.Length == 0)
                {
                    bind.Add(BindParameter.WithDefaultValue(parameter));

                    return bind;
                }

                if (options.TryGetTypeInfo(parameter.ParameterType, out var typeInfo))
                {
                    resolvedParameter = await JsonSerializer.DeserializeAsync(body, typeInfo).ConfigureAwait(false);
                }
                else
                {
                    resolvedParameter = await JsonSerializer.DeserializeAsync(body, parameter.ParameterType, options).ConfigureAwait(false);
                }
            }
            else
            {
                var buffer = new byte[4096];
                var count = await body.ReadAsync(buffer).ConfigureAwait(false);

                if (count == 0)
                {
                    bind.Add(BindParameter.WithDefaultValue(parameter));

                    return bind;
                }

                var charBuffer = new char[count];
                Encoding.UTF8.GetChars(buffer, 0, count, charBuffer, 0);

                if (options.TryGetTypeInfo(parameter.ParameterType, out var typeInfo))
                {
                    resolvedParameter = JsonSerializer.Deserialize(charBuffer, typeInfo);
                }
                else
                {
                    resolvedParameter = JsonSerializer.Deserialize(charBuffer, parameter.ParameterType, options);
                }
            }

            bind.Add(new(parameter.Name, resolvedParameter));

            return bind;
        }

        bind.Add(BindParameter.Empty);

        return bind;
    }
}
