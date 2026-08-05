using Docker.DotNet;
using Docker.DotNet.Models;

namespace IntegrationTests
{
    internal static class TestHelpers
    {
        public static async Task StopAndRemoveContainer(IDockerClient dockerClient, string name)
        {
            var containers = await GetContainers(dockerClient);
            var container = containers.FirstOrDefault(c => c.Names.Any(n => n == $"/{name}"));
            if (container == null)
                return;

            await dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters());
            await dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true });
        }

        public static async Task<IList<ContainerListResponse>> GetContainers(IDockerClient dockerClient)
        {
            return await dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });
        }
    }
}