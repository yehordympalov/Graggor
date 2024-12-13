using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;

namespace AttributeApi.Tests.InMemoryApi.Models;

[JsonConverter(typeof(UserJsonConverter))]
public class User(Guid id, string name, string username, string password)
{
    public Guid Id { get; set; } = id;

    public string Name { get; set; } = name;

    public string Password { get; set; } = password;

    public string Username { get; set; } = username;

    public override bool Equals(object? obj)
    {
        if (obj is not User user)
        {
            return false;
        }

        return Id.Equals(user.Id) && Name.Equals(user.Name) && Password.Equals(user.Password) && Username.Equals(user.Username);
    }

    public bool EqualsWithoutId(User user) => Name.Equals(user.Name) && Password.Equals(user.Password) && Username.Equals(user.Username);

    public override int GetHashCode() => Id.GetHashCode() + Name.GetHashCode() + Password.GetHashCode() + Username.GetHashCode();

    public User CloneWithNewId() => new(Guid.CreateVersion7(), Name, Username, Password);

    public User CloneWithNewId(Guid id) => new(id, Name, Username, Password);

    public User Clone() => new(Id, Name, Username, Password);
}
