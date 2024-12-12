using AttributeApi.Tests.InMemoryApi.Build;
using AttributeApi.Tests.InMemoryApi.Tests.Abstraction;

namespace AttributeApi.Tests.InMemoryApi.Tests;

public class SyncServiceTests(WebFactory factory) : AbstractServiceTests(factory)
{
    protected override string ServiceRoute => "sync/users";
}
