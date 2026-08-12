using Api.Models;
using ApiClient.Extensions;
using Core.Testing.Assertions;
using Docker.DotNet.Models;
using System.Net;
using Xunit;
using static AwesomeAssertions.AssertionExtensions;

namespace IntegrationTests.Tests;

public sealed class DeployTests : Test
{
    [Fact]
    public async Task InvalidRequest_ExpectedProblemDetails()
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
    public async Task ValidRequest_Latest_Returns200_AndStartsContainer()
    {
        await DeployAndAssert("test-project", "dev", "latest", "ghcr.io/michaeltg17/deployer:latest");
    }

    [Fact]
    public async Task ValidRequest_CommitTag_Returns200_AndStartsContainer()
    {
        await DeployAndAssert("test-project", "dev", "21ec91a", "ghcr.io/michaeltg17/deployer:21ec91a");
    }

    async Task DeployAndAssert(string project, string environment, string tag, string expectedImage)
    {
        var containerName = $"deployer-test-{tag}";
        await StopAndRemoveContainer(containerName);

        //When
        var response = await ApiClient.Deploy(new DeployRequest
        {
            Project = project,
            Environment = environment,
            Tag = tag,
        });

        //Then: expected response
        await response.ValidateOrThrow(HttpStatusCode.OK);

        //Then: expected container
        var containers = await GetContainers();
        var container = containers.FirstOrDefault(c => c.Names.Any(n => n == $"/{containerName}"));
        container.Should().NotBeNull();
        container.Image.Should().Be(expectedImage);
        container.State.Should().Be("running");

        //Then: expected env variables
        var inspect = await DockerClient.Containers.InspectContainerAsync(container.ID);
        inspect.Config.Env.Should().NotBeNull();
        inspect.Config.Env.Should().Contain("COMMON=COMMON_VALUE");
        inspect.Config.Env.Should().Contain("SECRET=SECRET_DEV");

        await StopAndRemoveContainer(containerName);
    }
}