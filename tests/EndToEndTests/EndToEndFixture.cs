using System.Net.Http.Headers;
using Api.Services;
using Docker.DotNet;
using Docker.DotNet.Models;
using Xunit;
using static AwesomeAssertions.AssertionExtensions;

namespace EndToEndTests;

public sealed class EndToEndFixture : IAsyncLifetime
{
    public string ContainerId { get; private set; } = string.Empty;
    public HttpClient HttpClient { get; private set; } = null!;
    public IDockerClient DockerClient { get; }
    public string ImageName => "deployer-e2e";

    public EndToEndFixture()
    {
        var config = new DockerClientConfiguration();
        DockerClient = config.CreateClient();
        config.Dispose();
    }

    public async ValueTask InitializeAsync()
    {
        var repoRoot = GetRepoRoot();

        await StopAndRemoveContainers("deployer-e2e", "deployer-test-*");

        var existingImage = (await DockerClient.Images.ListImagesAsync(new ImagesListParameters { All = true }))
            .FirstOrDefault(i => i.RepoTags.Any(t => t.StartsWith(ImageName + ":")));
        if (existingImage != null)
            await DockerClient.Images.DeleteImageAsync(existingImage.ID, new ImageDeleteParameters());

        await BuildImage(repoRoot);
        await StartContainer();
        await WaitForReady();
    }

    public async ValueTask DisposeAsync()
    {
        if (!string.IsNullOrEmpty(ContainerId))
        {
            try
            {
                await DockerClient.Containers.StopContainerAsync(ContainerId, new ContainerStopParameters());
            }
            catch { }
            try
            {
                await DockerClient.Containers.RemoveContainerAsync(ContainerId, new ContainerRemoveParameters { Force = true });
            }
            catch { }

            await StopAndRemoveContainers("deployer-e2e", "deployer-test-*");
        }

        var images = await DockerClient.Images.ListImagesAsync(new ImagesListParameters { All = true });
        var image = images.FirstOrDefault(i => i.RepoTags.Any(t => t.StartsWith(ImageName + ":")));
        if (image != null)
        {
            try { await DockerClient.Images.DeleteImageAsync(image.ID, new ImageDeleteParameters()); } catch { }
        }

        HttpClient.Dispose();
        DockerClient.Dispose();
    }

    async Task BuildImage(string repoRoot)
    {
        var processRunner = new ProcessRunner();
        var result = await processRunner.Run("docker", $"build -f Dockerfile -t {ImageName} .", repoRoot);
        result.ExitCode.Should().Be(0, $"Build failed:\n{result.Stdout}\n{result.Stderr}");
    }

    async Task StartContainer()
    {
        var processRunner = new ProcessRunner();
        var isLinux = Environment.OSVersion.Platform == PlatformID.Unix;
        var args = isLinux
            ? "run -d --name deployer-e2e --privileged -v /var/run/docker.sock:/var/run/docker.sock -e KeePassDbPath=/test/sandbox/secrets.kdbx -e KeePassDbPassword=test -e ProjectsDir=/test/sandbox/projects -e ThrowIfNoSecrets=false " + ImageName
            : "run -d --name deployer-e2e --privileged -p 0:8080 -v /var/run/docker.sock:/var/run/docker.sock -e KeePassDbPath=/test/sandbox/secrets.kdbx -e KeePassDbPassword=test -e ProjectsDir=/test/sandbox/projects -e ThrowIfNoSecrets=false " + ImageName;
        var result = await processRunner.Run("docker", args);
        result.ExitCode.Should().Be(0, $"Container start failed:\n{result.Stdout}\n{result.Stderr}");
        ContainerId = result.Stdout.TrimEnd('\r', '\n');

        await Task.Delay(2000);

        var inspect = DockerClient.Containers.InspectContainerAsync(ContainerId).Result;
        var containerIp = inspect.NetworkSettings.Networks["bridge"]?.IPAddress;

        HttpClient = new HttpClient
        {
            BaseAddress = new Uri(GetUrl()),
        };
        HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        string GetUrl()
        {
            if (isLinux && !string.IsNullOrEmpty(containerIp))
                return $"http://{containerIp}:8080";

            var hostPort = inspect.NetworkSettings.Ports["8080/tcp"]![0].HostPort;
            return $"http://localhost:{hostPort}";
        }
    }

    async Task WaitForReady()
    {
        for (var i = 0; i < 60; i++)
        {
            try
            {
                var response = await HttpClient.GetAsync("/test/GetOk");
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch
            {
            }
            await Task.Delay(1000);
        }
        throw new TimeoutException($"Container '{ContainerId}' did not become ready in time. Run 'docker logs {ContainerId}' for details.");
    }

    string GetRepoRoot()
    {
        var assemblyDir = Path.GetDirectoryName(typeof(EndToEndFixture).Assembly.Location)!;
        return Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", ".."));
    }

    async Task StopAndRemoveContainers(params string[] patterns)
    {
        var containers = await DockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });
        foreach (var pattern in patterns)
        {
            var matching = containers.Where(c => c.Names.Any(n =>
                (pattern.EndsWith("*") && n.StartsWith("/" + pattern.Substring(0, pattern.Length - 1), StringComparison.Ordinal)) ||
                n == "/" + pattern)).ToList();
            foreach (var container in matching)
            {
                try
                {
                    if (container.State == "running")
                        await DockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters());
                    await DockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true });
                }
                catch { }
            }
        }
    }
}