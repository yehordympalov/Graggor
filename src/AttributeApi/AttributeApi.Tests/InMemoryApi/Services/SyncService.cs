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
    public void AddUserAsync([FromBody] User? user)
    {
        if (user is null)
        {
            throw new AttributeApiException("Received empty user", TypedResults.BadRequest("Body is empty."));
        }

        _users.TryAdd(user.Id, user);
    }

    [Get("{id:guid}")]
    public User GetUserAsync([FromRoute] Guid id)
    {
        _users.TryGetValue(id, out var user);

        return user ?? throw new AttributeApiException($"User with ID {id} is not found", TypedResults.NotFound());
    }

    [Delete("{id:guid}")]
    public User DeleteUserAsync([FromRoute] Guid id)
    {
        _users.TryRemove(id, out var user);

        return user ?? throw new AttributeApiException($"User with ID {id} is not found", TypedResults.NotFound());
    }

    [Put("{id:guid}")]
    public User UpdateUserAsync([FromRoute] Guid id, [FromBody] User user)
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
    public User UpdateNameAsync([FromRoute] Guid id, [FromBody] string name)
    {
        _users.TryGetValue(id, out var value);

        if (value is null)
        {
            throw new AttributeApiException($"User with ID {id} is not found", TypedResults.NotFound());
        }

        value.Name = name;

        return value;
    }
}
