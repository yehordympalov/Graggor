using AttributeApi.Tests.InMemoryApi.Build;
using Microsoft.AspNetCore.TestHost;
using System.Text.Json;
using AttributeApi.Tests.InMemoryApi.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace AttributeApi.Tests.InMemoryApi.Tests.Abstraction;

public abstract class AbstractServiceTests : IClassFixture<WebFactory>
{
    protected readonly TestServer _testServer;
    protected readonly User _defaultUser;
    protected readonly User _updatedUser;
    protected readonly JsonSerializerOptions _jsonOptions;

    protected AbstractServiceTests(WebFactory webFactory)
    {
        _testServer = webFactory.Server;
        _jsonOptions = _testServer.Services.GetRequiredService<JsonSerializerOptions>();
        _defaultUser = new User(Guid.NewGuid(), "Default Name", "Default Username", "Default Password");
        _updatedUser = new User(_defaultUser.Id, "Updated Name", "Updated Username", "Updated Password");
    }

    [Fact]
    public async Task SendInvalidRequest_ShouldReturnMethodNotAllowed()
    {
        //Act

        var result = await _testServer.BuildRequest(ServiceRoute).SendAsync(HttpMethod.Connect.Method);

        //Assert

        Assert.Equal(HttpStatusCode.MethodNotAllowed, result.StatusCode);
    }

    [Fact]
    public async Task PostUserRequest_WhenRequestIsValid_ShouldReturnOK()
    {
        // Act

        var response = await AddUserAsync(_defaultUser);

        // Assert

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostUserRequest_WhenRequestHasNoBody_ShouldReturnBadRequest()
    {
        // Act

        var response = await _testServer.BuildRequest(ServiceRoute).PostAsync();

        // Assert

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUserRequest_WhenUserExists_ShouldReturnOKWithUser()
    {
        // Arrange

        await AddUserAsync(_defaultUser);

        // Act

        var response = await _testServer.BuildRequest(ServiceRoute, pattern: _defaultUser.Id.ToString()).GetAsync();

        // Assert

        var responseStream = await response.Content.ReadAsStreamAsync();
        var retrievedUser = await JsonSerializer.DeserializeAsync<User>(responseStream, _jsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(_defaultUser.Equals(retrievedUser));
    }

    [Fact]
    public async Task GetUserRequest_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Act

        var response = await _testServer.BuildRequest(ServiceRoute, pattern: Guid.NewGuid().ToString()).GetAsync();

        // Assert

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUserRequest_WhenUserExists_ShouldReturnOKWithUpdatedUser()
    {
        // Arrange

        await AddUserAsync(_defaultUser);

        // Act

        var response = await _testServer.BuildRequest(ServiceRoute, _updatedUser, _updatedUser.Id.ToString()).PutAsync();

        // Assert

        var responseStream = await response.Content.ReadAsStreamAsync();
        var updatedUser = await JsonSerializer.DeserializeAsync<User>(responseStream, _jsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(_updatedUser.Equals(updatedUser));
    }

    [Fact]
    public async Task UpdateUserRequest_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Act

        var response = await _testServer.BuildRequest(ServiceRoute, _updatedUser, _updatedUser.Id.ToString()).PutAsync();

        // Assert

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUserRequest_WhenUserExists_ShouldReturnOKWithDeletedUser()
    {
        // Arrange

        await AddUserAsync(_defaultUser);

        // Act

        var response = await _testServer.BuildRequest(ServiceRoute, pattern: _defaultUser.Id.ToString()).DeleteAsync();

        // Assert

        var responseStream = await response.Content.ReadAsStreamAsync();
        var deletedUser = await JsonSerializer.DeserializeAsync<User>(responseStream, _jsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(_defaultUser.Equals(deletedUser));
    }

    [Fact]
    public async Task DeleteUserRequest_FromQueryAsArray_WhenUsersExists_ShouldReturnOKWithDeletedUsers()
    {
        // Arrange

        var user1 = _defaultUser.CloneWithNewId();
        var user2 = _defaultUser.CloneWithNewId();
        var user3 = _defaultUser.CloneWithNewId();

        var list = new List<User>
        {
            user1, user2, user3
        };

        var query = '?' + string.Join('&', list.Select(user => $"ids={user.Id}"));

        await AddUserAsync(user1);
        await AddUserAsync(user2);
        await AddUserAsync(user3);

        // Act

        var response = await _testServer.BuildRequest(ServiceRoute + "/array" + query).DeleteAsync();

        // Assert

        var responseStream = await response.Content.ReadAsStreamAsync();
        var deletedUsers = await JsonSerializer.DeserializeAsync<List<User>>(responseStream, _jsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(deletedUsers.Count == 3);
    }

    [Fact]
    public async Task DeleteUserRequest_FromQueryAsArray_WhenUsersAreNotFound_ShouldReturnNotFound()
    {
        // Arrange

        var user1 = _defaultUser.CloneWithNewId();
        var user2 = _defaultUser.CloneWithNewId();
        var user3 = _defaultUser.CloneWithNewId();

        var list = new List<User>
        {
            user1, user2, user3
        };

        var query = '?' + string.Join('&', list.Select(user => $"ids={Guid.CreateVersion7()}"));

        await AddUserAsync(user1);
        await AddUserAsync(user2);
        await AddUserAsync(user3);

        // Act

        var response = await _testServer.BuildRequest(ServiceRoute + "/array" + query).DeleteAsync();

        // Assert

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUserRequest_FromQueryAsEnumerable_WhenUsersExists_ShouldReturnOKWithDeletedUsers()
    {
        // Arrange

        var user1 = _defaultUser.CloneWithNewId();
        var user2 = _defaultUser.CloneWithNewId();
        var user3 = _defaultUser.CloneWithNewId();

        var list = new List<User>
        {
            user1, user2, user3
        };

        var query = '?' + string.Join('&', list.Select(user => $"ids={user.Id}"));

        await AddUserAsync(user1);
        await AddUserAsync(user2);
        await AddUserAsync(user3);

        // Act

        var response = await _testServer.BuildRequest(ServiceRoute + "/enumerable" + query).DeleteAsync();

        // Assert

        var responseStream = await response.Content.ReadAsStreamAsync();
        var deletedUsers = await JsonSerializer.DeserializeAsync<List<User>>(responseStream, _jsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(deletedUsers.Count == 3);
    }

    [Fact]
    public async Task DeleteUserRequest_FromQueryAsEnumerable_WhenUsersAreNotFound_ShouldReturnNotFound()
    {
        // Arrange

        var user1 = _defaultUser.CloneWithNewId();
        var user2 = _defaultUser.CloneWithNewId();
        var user3 = _defaultUser.CloneWithNewId();

        var list = new List<User>
        {
            user1, user2, user3
        };

        var query = '?' + string.Join('&', list.Select(user => $"ids={Guid.CreateVersion7()}"));

        await AddUserAsync(user1);
        await AddUserAsync(user2);
        await AddUserAsync(user3);

        // Act

        var response = await _testServer.BuildRequest(ServiceRoute + "/enumerable" + query).DeleteAsync();

        // Assert

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUserRequest_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Act

        var response = await _testServer.BuildRequest(ServiceRoute, pattern: Guid.NewGuid().ToString()).DeleteAsync();

        // Assert

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutUserRequest_WhenUserExists_ShouldReturnOKWithUpdatedName()
    {
        // Arrange

        await AddUserAsync(_defaultUser);
        var updatedName = "New Name";

        // Act

        var response = await _testServer.BuildRequest(ServiceRoute, updatedName, _defaultUser.Id + "/name").PatchAsync();

        // Assert

        var responseStream = await response.Content.ReadAsStreamAsync();
        var updatedUser = await JsonSerializer.DeserializeAsync<User>(responseStream, _jsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(updatedName, updatedUser.Name);
    }

    [Fact]
    public async Task PutUserRequest_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Act

        var response = await _testServer.BuildRequest("New Name", Guid.NewGuid() + "/name").PatchAsync();

        // Assert

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    protected abstract string ServiceRoute { get; }

    protected Task<HttpResponseMessage> AddUserAsync(User user) => _testServer.BuildRequest(ServiceRoute, user).PostAsync();
}
