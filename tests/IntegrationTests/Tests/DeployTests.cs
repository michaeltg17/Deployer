using Api.Models;
using ApiClient.Validators;
using Core.Testing.Assertions;
using Xunit;
using static AwesomeAssertions.AssertionExtensions;

namespace IntegrationTests.Tests;

public sealed class DeployTests : Test
{
    [Fact]
    public async Task RequestMissingFields_ExpectedProblemDetails()
    {
        //When
        var response = await ApiClient.Deploy(new DeployRequest());

        //Then
        await ProblemDetailsAssertions.AssertValidationException(
            response,
            "/",
            new Dictionary<string, string[]>
            {
                { "project", ["'project' must not be empty."] },
                { "environment", ["'environment' must not be empty."] },
                { "tag", ["'tag' must not be empty."] }
            });
    }

    [Fact]
    public async Task ValidRequest_NoComposeFile_Returns400()
    {
        var response = await ApiClient.Deploy(new DeployRequest
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
            var response = await ApiClient.Deploy(new DeployRequest
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

        var response = await ApiClient.Deploy(new DeployRequest
        {
            Project = project,
            Environment = environment,
            Tag = tag,
        });
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var containers = await GetContainers();
        var container = containers.FirstOrDefault(c => c.Names.Any(n => n == $"/{containerName}"));
        container.Should().NotBeNull();
        container.Image.Should().Be(expectedImage);

        var inspect = await DockerClient.Containers.InspectContainerAsync(container.ID);
        inspect.Config.Env.Should().NotBeNull();
        inspect.Config.Env.Should().Contain("COMMON=COMMON_VALUE");
        inspect.Config.Env.Should().Contain("SECRET=SECRET_DEV");
        await StopAndRemoveContainer(containerName);
    }
}