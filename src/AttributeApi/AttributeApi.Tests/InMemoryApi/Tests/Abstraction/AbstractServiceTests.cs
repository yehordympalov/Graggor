using AttributeApi.Tests.InMemoryApi.Build;
using Microsoft.AspNetCore.TestHost;
using System.Text.Json;
using AttributeApi.Tests.InMemoryApi.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using AttributeApi.Services.Core;

namespace AttributeApi.Tests.InMemoryApi.Tests.Abstraction;

public abstract class AbstractServiceTests : IClassFixture<WebFactory>
{
    protected readonly TestServer _testServer;
    protected readonly User _defaultUser;
    protected readonly JsonSerializerOptions _jsonOptions;

    protected AbstractServiceTests(WebFactory webFactory)
    {
        _testServer = webFactory.Server;
        _jsonOptions = _testServer.Services.GetRequiredKeyedService<JsonSerializerOptions>(AttributeApiConfiguration.OPTIONS_KEY);
        _defaultUser = new User(default, "Default Name", "Default Username", "Default Password");
    }

    protected abstract string ServiceRoute { get; }

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

        var id = (await PostUserAndExtract(_defaultUser)).Id;

        // Act

        var response = await _testServer.BuildRequest(ServiceRoute, pattern: id.ToString()).GetAsync();

        // Assert

        var responseUser = await response.Content.ExtractUserAsync(_jsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(_defaultUser.EqualsWithoutId(responseUser));
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

        var user = await PostUserAndExtract(_defaultUser);
        var updatedUser = user.Clone();
        updatedUser.Name = "updated";

        // Act

        var response = await _testServer.BuildRequest(ServiceRoute, updatedUser, updatedUser.Id.ToString()).PutAsync();

        // Assert

        var responseUser = await response.Content.ExtractUserAsync(_jsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(responseUser.Equals(updatedUser));
    }

    [Fact]
    public async Task UpdateUserRequest_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Act

        var user = _defaultUser.CloneWithNewId();

        var response = await _testServer.BuildRequest(ServiceRoute, user, user.Id.ToString()).PutAsync();

        // Assert

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUserRequest_WhenUserExists_ShouldReturnOKWithDeletedUser()
    {
        // Arrange

        var id = (await PostUserAndExtract(_defaultUser)).Id;

        // Act

        var response = await _testServer.BuildRequest(ServiceRoute, pattern: id.ToString()).DeleteAsync();

        // Assert

        var deletedUser = await response.Content.ExtractUserAsync(_jsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(_defaultUser.EqualsWithoutId(deletedUser));
    }

    [Fact]
    public async Task DeleteUserRequest_FromQueryAsArray_WhenUsersExists_ShouldReturnOKWithDeletedUsers()
    {
        // Arrange

        var user1 = _defaultUser.Clone();
        var user2 = _defaultUser.Clone();
        var user3 = _defaultUser.Clone();

        var response1 = await AddUserAsync(user1);
        var response2 = await AddUserAsync(user2);
        var response3 = await AddUserAsync(user3);

        var id1 = (await response1.Content.ExtractUserAsync(_jsonOptions)).Id;
        var id2 = (await response2.Content.ExtractUserAsync(_jsonOptions)).Id;
        var id3 = (await response3.Content.ExtractUserAsync(_jsonOptions)).Id;

        var list = new List<Guid>
        {
            id1, id2, id3
        };

        var query = '?' + string.Join('&', list.Select(id => $"ids={id}"));

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

        var id = (await PostUserAndExtract(_defaultUser)).Id;
        var updatedName = "New Name";

        // Act

        var response = await _testServer.BuildRequest(ServiceRoute, updatedName, id + "/name").PatchAsync();

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

    protected Task<HttpResponseMessage> AddUserAsync(User user) => _testServer.BuildRequest(ServiceRoute, user).PostAsync();

    protected async Task<User> PostUserAndExtract(User user)
    {
        var response = await AddUserAsync(user);

        return await response.Content.ExtractUserAsync(_jsonOptions);
    }
}
