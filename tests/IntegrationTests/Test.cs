using Docker.DotNet;
using Xunit;

namespace IntegrationTests
{
    public abstract class Test : IClassFixture<TestFixture>
    {
        public ApiClient.ApiClient ApiClient { get; private set; } = default!;
        internal TestFixture TestFixture { get; set; } = default!;
        internal IDockerClient DockerClient { get; set; } = default!;

        public Test()
        {
            var config = new DockerClientConfiguration();
            DockerClient = config.CreateClient();
        }

        public virtual ValueTask Initialize()
        {
            ApiClient = new(TestFixture.CreateClient());
            return ValueTask.CompletedTask;
        }
    }
}
