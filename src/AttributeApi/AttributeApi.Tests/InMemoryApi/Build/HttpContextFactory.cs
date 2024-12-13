using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace AttributeApi.Tests.InMemoryApi.Build;

public class HttpContextFactory : IHttpContextFactory
{
    public HttpContext Create(IFeatureCollection featureCollection) => new DefaultHttpContext(featureCollection)
    {
        Request =
        {
            Body = new MemoryStream()
        }
    };

    public void Dispose(HttpContext httpContext)
    {
        
    }
}
