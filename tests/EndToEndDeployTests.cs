using System.Net;
using System.Text;
using System.Text.Json;
using Api.Models;
using Docker.DotNet;
using Docker.DotNet.Models;
using Xunit;
using static AwesomeAssertions.AssertionExtensions;

namespace Tests;

public sealed class EndToEndDeployTests : IClassFixture<EndToEndFixture>
{
    readonly HttpClient client;
    readonly IDockerClient dockerClient;

    public EndToEndDeployTests(EndToEndFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        client = fixture.HttpClient;
        dockerClient = fixture.DockerClient;
    }

    [Fact]
    public async Task ValidRequest_BuiltImage_Returns200()
    {
        var containerName = "deployer-test-latest";
        await StopAndRemoveContainer(containerName);

        var body = new DeployRequest
        {
            Project = "test-project",
            Environment = "dev",
            Tag = "latest",
        };
        using var content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(new Uri("/", UriKind.Relative), content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var containers = await GetContainers();
        var container = containers.FirstOrDefault(c => c.Names.Any(n => n == $"/{containerName}"));
        container.Should().NotBeNull();

        var inspect = await dockerClient.Containers.InspectContainerAsync(container.ID);
        inspect.Config.Env.Should().Contain("COMMON=COMMON_VALUE");

        await StopAndRemoveContainer(containerName);
    }

    [Fact]
    public async Task ValidRequest_CommitTag_BuiltImage_Returns200()
    {
        var containerName = "deployer-test-21ec91a";
        await StopAndRemoveContainer(containerName);

        var body = new DeployRequest
        {
            Project = "test-project",
            Environment = "dev",
            Tag = "21ec91a",
        };
        using var content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(new Uri("/", UriKind.Relative), content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var containers = await GetContainers();
        var container = containers.FirstOrDefault(c => c.Names.Any(n => n == $"/{containerName}"));
        container.Should().NotBeNull();

        container.Image.Should().Be("ghcr.io/michaeltg17/deployer:21ec91a");

        await StopAndRemoveContainer(containerName);
    }
}