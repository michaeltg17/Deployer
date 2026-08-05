using Docker.DotNet;
using Serilog;
using Xunit;

namespace IntegrationTests
{
    [Collection(nameof(TestCollectionFixture))]
    public abstract class Test : IAsyncLifetime
    {
        public ApiClient.ApiClient ApiClient { get; private set; } = default!;
        internal TestFixture TestFixture { get; set; } = default!;
        internal IDockerClient DockerClient { get; set; } = default!;

        public Test()
        {
            var config = new DockerClientConfiguration();
            DockerClient = config.CreateClient();
        }

        public async ValueTask DisposeAsync()
        {
            TestFixture.FlushLogger();
        }

        public virtual ValueTask Initialize()
        {
            ApiClient = new(TestFixture.CreateClient());
            return ValueTask.CompletedTask;
        }

        public ValueTask InitializeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
