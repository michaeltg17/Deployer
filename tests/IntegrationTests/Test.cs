using Docker.DotNet;
using Docker.DotNet.Models;
using Xunit;

namespace IntegrationTests
{
    [Collection(nameof(TestCollectionFixture))]
    public abstract class Test : IAsyncLifetime
    {
        public ApiClient.ApiClient ApiClient { get; private set; } = default!;
        internal TestFixture TestFixture { get; set; } = default!;
        internal IDockerClient DockerClient { get; set; } = default!;

        protected Test()
        {
            var config = new DockerClientConfiguration();
            DockerClient = config.CreateClient();
        }

        public ValueTask DisposeAsync()
        {
            TestFixture.FlushLogger();
            return ValueTask.CompletedTask;
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

        public async Task StopAndRemoveContainer(string name)
        {
            var containers = await GetContainers();
            var container = containers.FirstOrDefault(c => c.Names.Any(n => n == $"/{name}"));
            if (container == null)
                return;

            await DockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters());
            await DockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true });
        }

        public async Task<IList<ContainerListResponse>> GetContainers()
        {
            return await DockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });
        }
    }
}
