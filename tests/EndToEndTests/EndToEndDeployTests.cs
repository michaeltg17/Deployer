using Api.Models;
using Docker.DotNet;
using Xunit;
using static AwesomeAssertions.AssertionExtensions;

namespace EndToEndTests;

public sealed class EndToEndDeployTests : IClassFixture<EndToEndFixture>
{
    readonly ApiClient.ApiClient apiClient;
    readonly IDockerClient dockerClient;

    public EndToEndDeployTests(EndToEndFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        apiClient = new ApiClient.ApiClient(fixture.HttpClient);
        dockerClient = fixture.DockerClient;
    }

    [Fact]
    public async Task ValidRequest_BuiltImage_Returns200()
    {
        var containerName = "deployer-test-latest";
        await TestHelpers.StopAndRemoveContainer(dockerClient, containerName);

        var response = await apiClient.Deploy(new DeployRequest
        {
            Project = "test-project",
            Environment = "dev",
            Tag = "latest",
        });

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var containers = await TestHelpers.GetContainers(dockerClient);
        var container = containers.FirstOrDefault(c => c.Names.Any(n => n == $"/{containerName}"));
        container.Should().NotBeNull();

        var inspect = await dockerClient.Containers.InspectContainerAsync(container.ID, TestContext.Current.CancellationToken);
        inspect.Config.Env.Should().Contain("COMMON=COMMON_VALUE");

        await TestHelpers.StopAndRemoveContainer(dockerClient, containerName);
    }

    [Fact]
    public async Task ValidRequest_CommitTag_BuiltImage_Returns200()
    {
        var containerName = "deployer-test-21ec91a";
        await TestHelpers.StopAndRemoveContainer(dockerClient, containerName);

        var response = await apiClient.Deploy(new DeployRequest
        {
            Project = "test-project",
            Environment = "dev",
            Tag = "21ec91a",
        });

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var containers = await TestHelpers.GetContainers(dockerClient);
        var container = containers.FirstOrDefault(c => c.Names.Any(n => n == $"/{containerName}"));
        container.Should().NotBeNull();

        container.Image.Should().Be("ghcr.io/michaeltg17/deployer:21ec91a");

        await TestHelpers.StopAndRemoveContainer(dockerClient, containerName);
    }

    [Fact]
    public async Task ValidRequest_NoSecrets_DisabledSetting_Returns200()
    {
        var containerName = "deployer-test-no-secrets-e2e";
        await TestHelpers.StopAndRemoveContainer(dockerClient, containerName);

        var response = await apiClient.Deploy(new DeployRequest
        {
            Project = "no-secrets",
            Environment = "dev",
            Tag = "no-secrets-e2e",
        });

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        await TestHelpers.StopAndRemoveContainer(dockerClient, containerName);
    }
}