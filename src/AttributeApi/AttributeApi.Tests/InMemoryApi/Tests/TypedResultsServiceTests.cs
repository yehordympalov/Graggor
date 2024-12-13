using AttributeApi.Tests.InMemoryApi.Build;
using AttributeApi.Tests.InMemoryApi.Tests.Abstraction;

namespace AttributeApi.Tests.InMemoryApi.Tests;

public class TypedResultsServiceTests(WebFactory factory) : AbstractServiceTests(factory)
{
    protected override string ServiceRoute => "async/typedResults/users";
}
