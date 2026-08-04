using System.Net;
using System.Text;
using System.Text.Json;
using Api.Models;
using Docker.DotNet;
using Docker.DotNet.Models;
using Xunit;
using static AwesomeAssertions.AssertionExtensions;

namespace Tests;

public sealed class DeployTests : IClassFixture<TestFixture>
{
    private readonly HttpClient client;
    private readonly IDockerClient dockerClient;

    public DeployTests(TestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        client = fixture.CreateClient();
        dockerClient = fixture.DockerClient;
    }

    [Fact]
    public async Task MissingBody_Returns400()
    {
        //When
        var response = await client.PostAsync(new Uri("/", UriKind.Relative), null, TestContext.Current.CancellationToken);

        //Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InvalidBody_Returns400()
    {
        //Given
        using var content = new StringContent("not-json", Encoding.UTF8, "application/json");

        //When
        var response = await client.PostAsync(new Uri("/", UriKind.Relative), content, TestContext.Current.CancellationToken);

        //Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MissingEnvironment_Returns400()
    {
        //Given
        var body = new DeployRequest { Project = "test", Tag = "v1.0.0" };
        using var content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        //When
        var response = await client.PostAsync(new Uri("/", UriKind.Relative), content, TestContext.Current.CancellationToken);

        //Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MissingTag_Returns400()
    {
        //Given
        var body = new DeployRequest { Project = "test", Environment = "dev" };
        using var content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        //When
        var response = await client.PostAsync(new Uri("/", UriKind.Relative), content, TestContext.Current.CancellationToken);

        //Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ValidRequest_NoComposeFile_Returns400()
    {
        var body = new DeployRequest
        {
            Project = "test",
            Environment = "dev",
            Tag = "v1.0.0"
        };
        using var content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(new Uri("/", UriKind.Relative), content, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        errorResponse.Should().Contain("docker-compose.yml");
    }

    [Fact]
    public async Task ValidRequest_EachEnvironment_Returns400()
    {
        foreach (var environment in new[] { "dev", "qa", "prod" })
        {
            var body = new DeployRequest
            {
                Project = "test",
                Environment = environment,
                Tag = "v1.0.0"
            };
            using var content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(new Uri("/", UriKind.Relative), content, TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

        var body = new DeployRequest
        {
            Project = project,
            Environment = environment,
            Tag = tag,
        };
        using var content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(new Uri("/", UriKind.Relative), content);

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

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