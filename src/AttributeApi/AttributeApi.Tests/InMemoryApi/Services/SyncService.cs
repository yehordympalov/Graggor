using AttributeApi.Attributes;
using AttributeApi.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using AttributeApi.Services.Interfaces;
using AttributeApi.Tests.InMemoryApi.Models;

namespace AttributeApi.Tests.InMemoryApi.Services;

[Api("api/v1/sync/users")]
public class SyncService : IService
{
    private readonly ConcurrentDictionary<Guid, User> _users = [];

    [Post]
    public void AddUser([FromBody] User? user)
    {
        if (user is null)
        {
            throw new AttributeApiException("Received empty user", TypedResults.BadRequest("Body is empty."));
        }

        if (!_users.TryAdd(user.Id, user))
        {
            throw new AttributeApiException("Duplication", TypedResults.BadRequest("duplication"));
        }
    }

    [Get("{id:guid}")]
    public User GetUser([FromRoute] Guid id)
    {
        _users.TryGetValue(id, out var user);

        return user ?? throw new AttributeApiException($"User with ID {id} is not found", TypedResults.NotFound());
    }

    [Delete("{id:guid}")]
    public User DeleteUser([FromRoute] Guid id)
    {
        _users.TryRemove(id, out var user);

        return user ?? throw new AttributeApiException($"User with ID {id} is not found", TypedResults.NotFound());
    }

    [Put("{id:guid}")]
    public User UpdateUser([FromRoute] Guid id, [FromBody] User user)
    {
        _users.TryGetValue(id, out var value);

        if (value is null)
        {
            throw new AttributeApiException($"User with ID {id} is not found", TypedResults.NotFound());
        }

        _users[id] = user;

        return user;
    }

    [Patch("{id:guid}/name")]
    public User UpdateName([FromRoute] Guid id, [FromBody] string name)
    {
        _users.TryGetValue(id, out var value);

        if (value is null)
        {
            throw new AttributeApiException($"User with ID {id} is not found", TypedResults.NotFound());
        }

        value.Name = name;

        return value;
    }

    [Delete("array")]
    public List<User> DeleteUsers([FromQuery] Guid[] ids)
    {
        var list = new List<User>(ids.Length);

        foreach (var id in ids)
        {
            if (_users.TryRemove(id, out var user))
            {
                list.Add(user);
            }
        }

        if (list.Count == 0)
        {
            throw new AttributeApiException("Users are not found", TypedResults.NotFound());
        }

        return list;
    }

    [Delete("enumerable")]
    public List<User> DeleteUsers([FromQuery] IEnumerable<Guid> ids)
    {
        var list = new List<User>(ids.Count());

        foreach (var id in ids)
        {
            if (_users.TryRemove(id, out var user))
            {
                list.Add(user);
            }
        }

        if (list.Count == 0)
        {
            throw new AttributeApiException("Users are not found", TypedResults.NotFound());
        }

        return list;
    }
}
