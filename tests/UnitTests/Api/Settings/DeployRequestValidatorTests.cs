using Api.Models;
using Api.Settings;
using Api.Validation;
using AwesomeAssertions;
using Core.Testing.Builders;
using FluentValidation.TestHelper;
using Xunit;

namespace UnitTests.Api.Settings;

public sealed class DeployRequestValidatorTests : IDisposable
{
    readonly string tempDir;
    readonly DeployRequestValidator validator;

    public DeployRequestValidatorTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var validProjectDir = Path.Combine(tempDir, "test-project");
        Directory.CreateDirectory(validProjectDir);
        File.WriteAllText(Path.Combine(validProjectDir, "docker-compose.yml"), "");

        var settings = new DeployerSettings
        {
            KeePassDbPath = "secrets.kdbx",
            KeePassDbPassword = "password",
            ThrowIfNoSecrets = true,
            ProjectsDir = tempDir
        };

        validator = new DeployRequestValidator(settings);
    }

    public void Dispose() => Directory.Delete(tempDir, true);

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData(" ", null)]
    [InlineData("nonexistent", "Docker compose file not found for project 'nonexistent': docker-compose.yml")]
    public void InvalidProject_Error(string? project, string? expectedMessage)
    {
        //Given
        var request = new DeployRequestBuilder().WithValues(r => r.Project = project).Build();

        //When
        var result = validator.TestValidate(request);

        //Then
        result.ShouldHaveValidationErrorFor(r => r.Project)
            .WithErrorMessage(expectedMessage ?? "'Project' must not be empty.")
            .Only();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void InvalidEnvironment_Error(string? environment)
    {
        //Given
        var request = new DeployRequestBuilder().WithValues(r => r.Environment = environment).Build();

        //When
        var result = validator.TestValidate(request);

        //Then
        result.ShouldHaveValidationErrorFor(r => r.Environment).WithErrorMessage("'Environment' must not be empty.").Only();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void InvalidTag_Error(string? tag)
    {
        //Given
        var request = new DeployRequestBuilder().WithValues(r => r.Tag = tag).Build();

        //When
        var result = validator.TestValidate(request);

        //Then
        result.ShouldHaveValidationErrorFor(r => r.Tag).WithErrorMessage("'Tag' must not be empty.").Only();
    }

    [Fact]
    public void Valid_NoErrors()
    {
        //When
        var result = validator.TestValidate(new DeployRequestBuilder().Build());

        //Then
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AllNull_MultipleErrors()
    {
        //When
        var result = validator.TestValidate(new DeployRequest());

        //Then
        result.Errors.Should().HaveCount(3);
        result.ShouldHaveValidationErrorFor(r => r.Project).WithErrorMessage("'Project' must not be empty.");
        result.ShouldHaveValidationErrorFor(r => r.Environment).WithErrorMessage("'Environment' must not be empty.");
        result.ShouldHaveValidationErrorFor(r => r.Tag).WithErrorMessage("'Tag' must not be empty.");
    }
}
