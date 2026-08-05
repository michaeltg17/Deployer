using Xunit;

namespace IntegrationTests;

[CollectionDefinition(nameof(TestCollectionFixture))]
public class TestCollectionFixture : ICollectionFixture<TestFixture>
{
}