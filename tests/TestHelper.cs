using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tests
{
    internal class TestHelpers
    {
        async Task StopAndRemoveContainer(string name)
        {
            var containers = await GetContainers();
            var container = containers.FirstOrDefault(c => c.Names.Any(n => n == $"/{name}"));
            if (container == null)
                return;

            await dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters());
            await dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true });
        }

        Task<IList<ContainerListResponse>> GetContainers()
        {
            return dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });
        }
    }
}
