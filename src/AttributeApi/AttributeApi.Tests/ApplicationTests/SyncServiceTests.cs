using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using AttributeApi.Tests.InMemoryApi.Models;
using AttributeApi.Tests.InMemoryApi.Build;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Tests.ApplicationTests;

public class SyncServiceTests : IClassFixture<WebFactory>
{
    private readonly TestServer _testServer;
    private readonly User _defaultUser;
    private readonly User _updatedUser;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string SERVICE_ROUTE = "sync/users";

    public SyncServiceTests(WebFactory webFactory)
    {
        _testServer = webFactory.Server;
        _jsonOptions = _testServer.Services.GetRequiredService<JsonSerializerOptions>();
        _defaultUser = new User(Guid.NewGuid(), "Default Name", "Default Username", "Default Password");
        _updatedUser = new User(_defaultUser.Id, "Updated Name", "Updated Username", "Updated Password");
    }

    [Fact]
    public async Task AddUser_WhenRequestIsValid_ShouldReturnOK()
    {
        // Act

        var response = await AddUserAsync(_defaultUser);

        // Assert

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddUser_WhenRequestHasNoBody_ShouldReturnBadRequest()
    {
        // Act

        var response = await _testServer.BuildRequest(SERVICE_ROUTE).PostAsync();

        // Assert

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_WhenUserExists_ShouldReturnOKWithUser()
    {
        // Arrange

        await AddUserAsync(_defaultUser);

        // Act

        var response = await _testServer.BuildRequest(SERVICE_ROUTE, pattern: _defaultUser.Id.ToString()).GetAsync();

        // Assert

        var responseStream = await response.Content.ReadAsStreamAsync();
        var retrievedUser = JsonSerializer.Deserialize<User>(responseStream, _jsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(_defaultUser.Equals(retrievedUser));
    }

    [Fact]
    public async Task GetUser_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Act

        var response = await _testServer.BuildRequest(SERVICE_ROUTE, pattern: Guid.NewGuid().ToString()).GetAsync();

        // Assert

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_WhenUserExists_ShouldReturnOKWithUpdatedUser()
    {
        // Arrange

        await AddUserAsync(_defaultUser);

        // Act

        var response = await _testServer.BuildRequest(SERVICE_ROUTE, _updatedUser, _updatedUser.Id.ToString()).PutAsync();

        // Assert

        var responseStream = await response.Content.ReadAsStreamAsync();
        var updatedUser = JsonSerializer.Deserialize<User>(responseStream, _jsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(_updatedUser.Equals(updatedUser));
    }

    [Fact]
    public async Task UpdateUser_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Act

        var response = await _testServer.BuildRequest(SERVICE_ROUTE, _updatedUser, _updatedUser.Id.ToString()).PutAsync();

        // Assert

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_WhenUserExists_ShouldReturnOKWithDeletedUser()
    {
        // Arrange

        await AddUserAsync(_defaultUser);

        // Act

        var response = await _testServer.BuildRequest(SERVICE_ROUTE, pattern: _defaultUser.Id.ToString()).DeleteAsync();

        // Assert

        var responseStream = await response.Content.ReadAsStreamAsync();
        var deletedUser = JsonSerializer.Deserialize<User>(responseStream, _jsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(_defaultUser.Equals(deletedUser));
    }

    [Fact]
    public async Task DeleteUser_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Act

        var response = await _testServer.BuildRequest(SERVICE_ROUTE, pattern: Guid.NewGuid().ToString()).DeleteAsync();

        // Assert

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateName_WhenUserExists_ShouldReturnOKWithUpdatedName()
    {
        // Arrange

        await AddUserAsync(_defaultUser);
        var updatedName = "New Name";

        // Act

        var response = await _testServer.BuildRequest(SERVICE_ROUTE, updatedName, _defaultUser.Id + "/name").PatchAsync();

        // Assert

        var responseStream = await response.Content.ReadAsStreamAsync();
        var updatedUser = JsonSerializer.Deserialize<User>(responseStream, _jsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(updatedName, updatedUser.Name);
    }

    [Fact]
    public async Task UpdateName_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Act

        var response = await _testServer.BuildRequest("New Name", Guid.NewGuid() + "/name").PatchAsync();

        // Assert

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private Task<HttpResponseMessage> AddUserAsync(User user) => _testServer.BuildRequest(SERVICE_ROUTE, user).PostAsync();
}
