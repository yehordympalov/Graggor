﻿using System.Collections.Concurrent;
using AttributeApi.Attributes;
using AttributeApi.Exceptions;
using AttributeApi.Tests.InMemoryApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AttributeApi.Tests.InMemoryApi.Services;

[Api("api/v1/async/users")]
public class AsyncService
{
    private static readonly ConcurrentDictionary<Guid, User> _users = [];

    [Post]
    public Task<User> AddUserAsync([FromBody] User? user)
    {
        if (user is null)
        {
            throw new AttributeApiException("Received empty user", TypedResults.BadRequest("Body is empty."));
        }

        if (!_users.TryAdd(user.Id, user))
        {
            throw new AttributeApiException("Duplication", TypedResults.BadRequest("duplication"));
        }

        return Task.FromResult(user);
    }

    [Get("{id:guid}")]
    public Task<User> GetUserAsync([FromRoute] Guid id)
    {
        _users.TryGetValue(id, out var user);

        return user is null ? throw new AttributeApiException($"User with ID {id} is not found", TypedResults.NotFound()) : Task.FromResult(user);
    }

    [Delete("{id:guid}")]
    public Task<User> DeleteUserAsync([FromRoute] Guid id)
    {
        _users.TryRemove(id, out var user);

        return user is null ? throw new AttributeApiException($"User with ID {id} is not found", TypedResults.NotFound()) : Task.FromResult(user);
    }

    [Put("{id:guid}")]
    public Task<User> UpdateUserAsync([FromRoute] Guid id, [FromBody] User user)
    {
        _users.TryGetValue(id, out var value);

        if (value is null)
        {
            throw new AttributeApiException($"User with ID {id} is not found", TypedResults.NotFound());
        }

        _users[id] = user;

        return Task.FromResult(user);
    }

    [Patch("{id:guid}/name")]
    public Task<User> UpdateNameAsync([FromRoute] Guid id, [FromBody] string name)
    {
        _users.TryGetValue(id, out var value);

        if (value is null)
        {
            throw new AttributeApiException($"User with ID {id} is not found", TypedResults.NotFound());
        }

        value.Name = name;

        return Task.FromResult(value);
    }

    [Delete("array")]
    public Task<List<User>> DeleteUsersAsync([FromQuery] Guid[] ids)
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

        return Task.FromResult(list);
    }

    [Delete("enumerable")]
    public Task<List<User>> DeleteUsersAsync([FromQuery] IEnumerable<Guid> ids)
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

        return Task.FromResult(list);
    }
}
