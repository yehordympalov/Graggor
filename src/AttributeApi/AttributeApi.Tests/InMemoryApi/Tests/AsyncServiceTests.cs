using AttributeApi.Tests.InMemoryApi.Build;
using AttributeApi.Tests.InMemoryApi.Tests.Abstraction;

namespace AttributeApi.Tests.InMemoryApi.Tests;

public class AsyncServiceTests(WebFactory factory) : AbstractServiceTests(factory)
{
    protected override string ServiceRoute => "async/users";

}
