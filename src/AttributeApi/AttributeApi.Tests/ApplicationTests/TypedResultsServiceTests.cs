using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using AttributeApi.Tests.InMemoryApi.Models;
using AttributeApi.Tests.InMemoryApi.Build;
using System.Text.Json.Serialization.Metadata;
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
    public async Task SendRequest_WhenRequestMethodTypeIsWrong_ShouldReturnMethodNotAllowed()
    {
        //Act

        var result = await _server.BuildRequest().SendAsync(HttpMethod.Put.Method);

        //Assert

        Assert.Equal(HttpStatusCode.MethodNotAllowed, result.StatusCode);
    }

    [Fact]
    public async Task SendPostRequest_WhenRequestIsValid_ShouldReturnOK()
    {
        //Act

        var result = await PostDefaultUserAsync();

        //Assert 

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task SendPostRequest_WhenRequestHasNoBody_ShouldReturnBadRequest()
    {
        //Act

        var result = await _server.BuildRequest().PostAsync();

        //Assert

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task SendGetRequest_WhenRequestIsValid_ShouldReturnOKWithUser()
    {
        //Arrange

        var postResult = await PostDefaultUserAsync();

        //Act

        var result = await _server.BuildRequest(pattern: _defaultUser.Id.ToString()).GetAsync();

        //Assert

        Assert.Equal(HttpStatusCode.OK, postResult.StatusCode);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.True(_defaultUser.Equals(await postResult.Content.DeserializeFromContentAsync<User>(_options)));
    }

    [Fact]
    public async Task SendGetRequest_WhenRequestIsValidButUserIsNotFound_ShouldReturnNotFound()
    {
        //Arrange

        var postResult = await PostDefaultUserAsync();

        //Act

        var result = await _server.BuildRequest(pattern: Guid.CreateVersion7().ToString()).GetAsync();

        //Assert

        Assert.Equal(HttpStatusCode.OK, postResult.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task SendPutRequest_WhenRequestIsValid_ShouldUpdateUser()
    {
        //Arrange

        var postResult = await PostDefaultUserAsync();

        //Act

        var putResult = await _server.BuildRequest(_userToUpdate, _userToUpdate.Id.ToString()).PutAsync();

        //Assert

        Assert.Equal(HttpStatusCode.OK, postResult.StatusCode);
        Assert.Equal(HttpStatusCode.OK, putResult.StatusCode);
        Assert.True(_defaultUser.Equals(await postResult.Content.DeserializeFromContentAsync<User>(_options)));
        Assert.True(_userToUpdate.Equals(await putResult.Content.DeserializeFromContentAsync<User>(_options)));
    }

    private Task<HttpResponseMessage> PostDefaultUserAsync() => SendValidPostRequestAsync(_defaultUser);

    private Task<HttpResponseMessage> SendValidPostRequestAsync(User user) => _server.BuildRequest(user).PostAsync();
}
