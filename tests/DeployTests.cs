using Api.Models;
using Docker.DotNet;
using Docker.DotNet.Models;
using Xunit;
using static AwesomeAssertions.AssertionExtensions;

namespace Tests;

public sealed class DeployTests : IClassFixture<TestFixture>
{
    readonly ApiClient apiClient;
    readonly HttpClient client;
    readonly IDockerClient dockerClient;

    public DeployTests(TestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        client = fixture.CreateClient();
        apiClient = new ApiClient(client);
        dockerClient = fixture.DockerClient;
    }

    [Fact]
    public async Task MissingBody_Returns400()
    {
        var response = await client.PostAsync(new Uri("/", UriKind.Relative), null, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InvalidBody_Returns400()
    {
        using var content = new StringContent("not-json", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync(new Uri("/", UriKind.Relative), content, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MissingEnvironment_Returns400()
    {
        var response = await apiClient.Deploy(new DeployRequest { Project = "test", Tag = "v1.0.0" });
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MissingTag_Returns400()
    {
        var response = await apiClient.Deploy(new DeployRequest { Project = "test", Environment = "dev" });
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ValidRequest_NoComposeFile_Returns400()
    {
        var response = await apiClient.Deploy(new DeployRequest
        {
            Project = "test",
            Environment = "dev",
            Tag = "v1.0.0"
        });

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        errorResponse.Should().Contain("docker-compose.yml");
    }

    [Fact]
    public async Task ValidRequest_EachEnvironment_Returns400()
    {
        foreach (var environment in new[] { "dev", "qa", "prod" })
        {
            var response = await apiClient.Deploy(new DeployRequest
            {
                Project = "test",
                Environment = environment,
                Tag = "v1.0.0"
            });
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task ValidRequest_Latest_Returns200_AndStartsContainer()
    {
        await DeployAndVerify("test-project", "dev", "latest", "ghcr.io/michaeltg17/deployer:latest");
    }

    [Fact]
    public async Task ValidRequest_CommitTag_Returns200_AndStartsContainer()
    {
        await DeployAndVerify("test-project", "dev", "21ec91a", "ghcr.io/michaeltg17/deployer:21ec91a");
    }

    async Task DeployAndVerify(string project, string environment, string tag, string expectedImage)
    {
        var containerName = $"deployer-test-{tag}";
        await StopAndRemoveContainer(containerName);

        var response = await apiClient.Deploy(new DeployRequest
        {
            Project = project,
            Environment = environment,
            Tag = tag,
        });
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var containers = await dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true });
        var container = containers.FirstOrDefault(c => c.Names.Any(n => n == $"/{containerName}"));
        container.Should().NotBeNull();
        container.Image.Should().Be(expectedImage);

        var inspect = await dockerClient.Containers.InspectContainerAsync(container.ID);
        inspect.Config.Env.Should().NotBeNull();
        inspect.Config.Env.Should().Contain("COMMON=COMMON_VALUE");
        inspect.Config.Env.Should().Contain("SECRET=SECRET_DEV");
        await StopAndRemoveContainer(containerName);
    }

    async Task StopAndRemoveContainer(string name)
    {
        var containers = await dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true });
        var container = containers.FirstOrDefault(c => c.Names.Any(n => n == $"/{name}"));
        if (container == null)
            return;

        await dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters());
        await dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true });
    }
}