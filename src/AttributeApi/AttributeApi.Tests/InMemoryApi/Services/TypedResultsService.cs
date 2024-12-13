﻿using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using AttributeApi.Attributes;
using AttributeApi.Tests.InMemoryApi.Models;
using AttributeApi.Exceptions;

namespace AttributeApi.Tests.InMemoryApi.Services;

[Api("api/v1/async/typedResults/users")]
public class TypedResultsService(ILogger<TypedResultsService> logger)
{
    private static readonly ConcurrentDictionary<Guid, User> _users = [];

    [Post]
    public Task<Results<Ok<User>, BadRequest<string>>> AddUserAsync([FromBody] User? user)
    {
        if (user is null)
        {
            return Task.FromResult<Results<Ok<User>, BadRequest<string>>>(TypedResults.BadRequest("null"));
        }

        if (!_users.TryAdd(user.Id, user))
        {
            return Task.FromResult<Results<Ok<User>, BadRequest<string>>>(TypedResults.BadRequest("duplication"));
        }

        return Task.FromResult<Results<Ok<User>, BadRequest<string>>>(TypedResults.Ok(user));
    }

    [Get("{id:guid}")]
    public Task<Results<Ok<User>, NotFound>> GetUserAsync([FromRoute] Guid id)
    {
        _users.TryGetValue(id, out var user);

        return user is null ? Task.FromResult<Results<Ok<User>, NotFound>>(TypedResults.NotFound()) : Task.FromResult<Results<Ok<User>, NotFound>>(TypedResults.Ok(user));
    }

    [Delete("{id:guid}")]
    public Task<Results<Ok<User>, NotFound>> DeleteUserAsync([FromRoute] Guid id)
    {
        _users.TryRemove(id, out var user);

        return user is null ? Task.FromResult<Results<Ok<User>, NotFound>>(TypedResults.NotFound()) : Task.FromResult<Results<Ok<User>, NotFound>>(TypedResults.Ok(user));
    }

    [Put("{id:guid}")]
    public Task<Results<Ok<User>, BadRequest<string>, NotFound>> UpdateUserAsync([FromRoute] Guid id, [FromBody] User user)
    {
        if (!user.Id.Equals(id))
        {
            return Task.FromResult<Results<Ok<User>, BadRequest<string>, NotFound>>(TypedResults.BadRequest("id"));
        }

        _users.TryGetValue(id, out var value);

        if (value is null)
        {
            return Task.FromResult<Results<Ok<User>, BadRequest<string>, NotFound>>(TypedResults.NotFound());
        }

        _users[id] = user;

        return Task.FromResult<Results<Ok<User>, BadRequest<string>, NotFound>>(TypedResults.Ok(user));
    }

    [Patch("{id:guid}/name")]
    public Task<Results<Ok<User>, NotFound>> UpdateNameAsync([FromRoute] Guid id, [FromBody] string name)
    {
        _users.TryGetValue(id, out var value);

        if (value is null)
        {
            return Task.FromResult<Results<Ok<User>, NotFound>>(TypedResults.NotFound());
        }

        value.Name = name;

        return Task.FromResult<Results<Ok<User>, NotFound>>(TypedResults.Ok(value));
    }

    [Delete("array")]
    public Task<Results<Ok<List<User>>, NotFound>> DeleteUsersAsync([FromQuery] Guid[] ids)
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

        return Task.FromResult<Results<Ok<List<User>>, NotFound>>(TypedResults.Ok(list));
    }

    [Delete("enumerable")]
    public Task<Results<Ok<List<User>>, NotFound>> DeleteUsersAsync([FromQuery] IEnumerable<Guid> ids)
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

        return Task.FromResult<Results<Ok<List<User>>, NotFound>>(TypedResults.Ok(list));
    }
}
