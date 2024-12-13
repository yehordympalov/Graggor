using System.Text.Json;
using System.Text.Json.Serialization;

namespace AttributeApi.Tests.InMemoryApi.Models;

public class UserJsonConverter : JsonConverter<User>
{
    public override User Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of JSON object.");
        }

        var id = Guid.Empty;
        var name = string.Empty;
        var username = string.Empty;
        var password = string.Empty;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name.");
            }

            var propertyName = reader.GetString()!;
            reader.Read();

            switch (propertyName)
            {
                case nameof(User.Id):
                    id = reader.TokenType == JsonTokenType.String && Guid.TryParse(reader.GetString(), out var parsedId)
                        ? parsedId : Guid.Empty;
                    break;
                case nameof(User.Name):
                    name = reader.GetString() ?? string.Empty;
                    break;
                case nameof(User.Username):
                    username = reader.GetString() ?? string.Empty;
                    break;
                case nameof(User.Password):
                    password = reader.GetString() ?? string.Empty;
                    break;
                default:
                    throw new JsonException($"Unknown property: {propertyName}");
            }
        }

        if (id == Guid.Empty)
        {
            id = Guid.CreateVersion7();
        }

        return new User(id, name, username, password);
    }

    public override void Write(Utf8JsonWriter writer, User value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(nameof(User.Id), value.Id.ToString());
        writer.WriteString(nameof(User.Name), value.Name);
        writer.WriteString(nameof(User.Username), value.Username);
        writer.WriteString(nameof(User.Password), value.Password);
        writer.WriteEndObject();
    }
}
