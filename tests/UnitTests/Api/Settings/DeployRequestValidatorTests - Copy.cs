using System.Linq.Expressions;
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
    [InlineData(null, false, "'Project' must not be empty.")]
    [InlineData("", false, "'Project' must not be empty.")]
    [InlineData(" ", false, "'Project' must not be empty.")]
    [InlineData("nonexistent", false, "Docker compose file not found for project 'nonexistent': docker-compose.yml")]
    public void InvalidProject_ExpectedError(string? value, bool isValid, string? expectedMessage)
    {
        var request = new DeployRequestBuilder().WithValues(r => r.Project = value).Build();
        var result = validator.TestValidate(request);

        if (isValid) result.ShouldNotHaveAnyValidationErrors();
        else
        {
            result.ShouldHaveValidationErrorFor(r => r.Project).WithErrorMessage(expectedMessage!).Only();
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void InvalidEnvironment_ExpectedError(string? environment)
    {
        //Given
        var request = new DeployRequestBuilder().WithValues(r => r.Environment = environment).Build();

        //When
        var result = validator.TestValidate(request);

        //Then
        result.ShouldHaveValidationErrorFor(r => r.Environment).WithErrorMessage("'Environment' must not be empty.").Only();
    }

    [Theory]
    [InlineData(null, false, "'Tag' must not be empty.")]
    [InlineData("", false, "'Tag' must not be empty.")]
    [InlineData("   ", false, "'Tag' must not be empty.")]
    public void InvalidTag_ExpectedError(string? value, string? expectedMessage)
    {
        var request = new DeployRequestBuilder().WithValues(r => r.Tag = value).Build();
        var result = validator.TestValidate(request);

        if (isValid) result.ShouldNotHaveAnyValidationErrors();
        else
        {
            Expression<Func<DeployRequest, object?>> prop = r => r.Tag;
            result.ShouldHaveValidationErrorFor(r => r.Tag).WithErrorMessage(expectedMessage!).Only();
        }
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
    public void AllNull_ReturnsMultipleErrors()
    {
        //When
        var result = validator.TestValidate(new DeployRequest());

        //Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
    }
}
