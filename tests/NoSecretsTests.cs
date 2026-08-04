using Api.Models;
using Docker.DotNet;
using Xunit;
using static AwesomeAssertions.AssertionExtensions;

namespace Tests;

public sealed class NoSecretsTests : IClassFixture<ThrowIfNoSecretsFixture>
{
    readonly ApiClient.ApiClient apiClient;
    readonly IDockerClient dockerClient;

    public NoSecretsTests(ThrowIfNoSecretsFixture fixture)
    {
        apiClient = new ApiClient.ApiClient(fixture.CreateClient());
        dockerClient = fixture.DockerClient;
    }

    [Fact]
    public async Task ValidRequest_NoSeecretdsFoundInDb_ThrowsNoSecretsFoundException()
    {
        var containerName = "deployer-test-no-secrets";
        await TestHelpers.StopAndRemoveContainer(dockerClient, containerName);

        var response = await apiClient.Deploy(new DeployRequest
        {
            Project = "no-secrets",
            Environment = "dev",
            Tag = "no-secrets",
        });

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.Should().Contain("NoSecretsFoundException");
        content.Should().Contain("no-secrets");
        content.Should().Contain("dev");

        await TestHelpers.StopAndRemoveContainer(dockerClient, containerName);
    }
}