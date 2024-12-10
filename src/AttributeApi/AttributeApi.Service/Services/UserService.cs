using System.Collections.Concurrent;
using AttributeApi.Core.Attributes;
using AttributeApi.Core.Services.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AttributeApi.Service.Services;

[Api("/api/v1/users")]
public class UserService(ILogger<UserService> logger) : IService
{
    private readonly ConcurrentBag<User> _users = [];

    [Post]
    public Task<Results<Ok, BadRequest<string>>> AddUserAsync([FromBody] User? user)
    {
        if (user is null)
        {
            return Task.FromResult<Results<Ok, BadRequest<string>>>(TypedResults.BadRequest("null"));
        }

        _users.Add(user);

        return Task.FromResult<Results<Ok, BadRequest<string>>>(TypedResults.Ok());
    }

    [Get("{id:guid}")]
    public Task<Results<Ok<User>, NotFound>> GetUserAsync([FromRoute] Guid id)
    {
        var entity = _users.FirstOrDefault(user => user.Id == id);

        return entity is null ? Task.FromResult<Results<Ok<User>, NotFound>>(TypedResults.NotFound()) : Task.FromResult<Results<Ok<User>, NotFound>>(TypedResults.Ok(entity));
    }
}

public record User(Guid Id, string Username, string Password);
