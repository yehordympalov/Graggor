using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using AttributeApi.Tests.InMemoryApi.Models;
using AttributeApi.Tests.InMemoryApi.Build;
using Microsoft.Extensions.DependencyInjection;

namespace AttributeApi.Tests.ApplicationTests;

public class TypedResultsServiceTests : IClassFixture<WebFactory>
{
    private readonly TestServer _server;
    private readonly User _defaultUser;
    private readonly User _userToUpdate;
    private readonly JsonSerializerOptions _options;

    public TypedResultsServiceTests(WebFactory factory)
    {
        _server = factory.Server;
        _options = _server.Services.GetRequiredService<JsonSerializerOptions>();
        _defaultUser = new User(Guid.CreateVersion7(), "name", "username", "123");
        _userToUpdate = new User(_defaultUser.Id, "New name", "New username", "456");
    }

    [Fact]
    public async Task SendAsync_WhenRequestMethodTypeIsWrong_ShouldReturnMethodNotAllowed()
    {
        //Act

        var result = await _server.BuildRequest().PutAsync();

        //Assert

        Assert.Equal(HttpStatusCode.MethodNotAllowed, result.StatusCode);
    }

    [Fact]
    public async Task PostAsync_WhenRequestIsValid_ShouldReturnOK()
    {
        //Act

        var result = await PostDefaultEntityAsync();

        //Assert 

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task PostAsync_WhenRequestHasNoBody_ShouldReturnBadRequest()
    {
        //Act

        var result = await _server.BuildRequest().PostAsync();

        //Assert

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task GetAsync_WhenRequestIsValid_ShouldReturnOKWithUser()
    {
        //Arrange

        var postResult = await PostDefaultEntityAsync();

        //Act

        var result = await _server.BuildRequest(pattern: _defaultUser.Id.ToString()).GetAsync();

        //Assert

        var stream = await result.Content.ReadAsStreamAsync();
        var user = await JsonSerializer.DeserializeAsync<User>(stream, _options);

        Assert.Equal(HttpStatusCode.OK, postResult.StatusCode);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.True(_defaultUser.Equals(user));
    }

    [Fact]
    public async Task GetAsync_WhenRequestIsValidButUserIsNotFound_ShouldReturnNotFound()
    {
        //Arrange

        var postResult = await PostDefaultEntityAsync();

        //Act

        var result = await _server.BuildRequest(pattern: Guid.CreateVersion7().ToString()).GetAsync();

        //Assert

        Assert.Equal(HttpStatusCode.OK, postResult.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task PutAsync_WhenRequestIsValid_ShouldUpdateUser()
    {
        //Arrange

        var postResult = await PostDefaultEntityAsync();

        //Act

        var putResult = await _server.BuildRequest(_userToUpdate, _userToUpdate.Id.ToString()).PutAsync();

        //Assert

        Assert.Equal(HttpStatusCode.OK, postResult.StatusCode);
        Assert.Equal(HttpStatusCode.OK, putResult.StatusCode);

        var stream = await putResult.Content.ReadAsStreamAsync();
        var user = await JsonSerializer.DeserializeAsync<User>(stream, _options);

        Assert.True(_userToUpdate.Equals(user));
    }

    [Fact]
    public async Task PutAsync_WhenEntityIsNotFound_ShouldReturnNotFound()
    {
        //Arrange

        var postResult = await PostDefaultEntityAsync();
        var user = _defaultUser.CloneWithNewId();

        //Act

        var putResult = await _server.BuildRequest(user, user.Id.ToString()).PutAsync();

        //Assert

        Assert.Equal(HttpStatusCode.OK, postResult.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, putResult.StatusCode);
    }

    [Fact]
    public async Task PatchAsync_WhenEntityIsFound_ShouldReturnOK()
    {
        //Arrange

        var postResult = await PostDefaultEntityAsync();
        var name = "newName";

        //Act

        var patchResult = await _server.BuildRequest(name, _defaultUser.Id + "/name").PatchAsync();

        //Assert

        Assert.Equal(HttpStatusCode.OK, postResult.StatusCode);
        Assert.Equal(HttpStatusCode.OK, patchResult.StatusCode);

        var stream = await patchResult.Content.ReadAsStreamAsync();
        var user = await JsonSerializer.DeserializeAsync<User>(stream, _options);

        Assert.True(user.Name.Equals(name) && user.Id.Equals(_defaultUser.Id) && user.Password.Equals(_defaultUser.Password) && user.Username.Equals(_defaultUser.Username));
    }

    [Fact]
    public async Task PatchAsync_WhenEntityIsNotFound_ShouldReturnNotFound()
    {
        //Arrange

        var postResult = await PostDefaultEntityAsync();
        var name = "newName";

        //Act

        var patchResult = await _server.BuildRequest(name, Guid.CreateVersion7() + "/name").PatchAsync();

        //Assert

        Assert.Equal(HttpStatusCode.OK, postResult.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, patchResult.StatusCode);
    }

    private Task<HttpResponseMessage> PostDefaultEntityAsync() => SendValidPostRequestAsync(_defaultUser);

    private Task<HttpResponseMessage> SendValidPostRequestAsync(User user) => _server.BuildRequest(user).PostAsync();
}
