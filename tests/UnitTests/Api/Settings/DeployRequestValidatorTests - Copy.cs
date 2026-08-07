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
    [InlineData("test-project", true, null)]
    [InlineData(null, false, "'Project' must not be empty.")]
    [InlineData("", false, "'Project' must not be empty.")]
    [InlineData(" ", false, "'Project' must not be empty.")]
    [InlineData("nonexistent", false, "Docker compose file not found for project 'nonexistent': docker-compose.yml")]
    public void Project_ShouldValidate(string? value, bool isValid, string? expectedMessage)
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
    [InlineData("dev", true, null)]
    [InlineData(null, false, "'Environment' must not be empty.")]
    [InlineData("", false, "'Environment' must not be empty.")]
    [InlineData("   ", false, "'Environment' must not be empty.")]
    public void Environment_ShouldValidate(string? value, bool isValid, string? expectedMessage)
    {
        var request = new DeployRequestBuilder().WithValues(r => r.Environment = value).Build();
        var result = validator.TestValidate(request);

        if (isValid) result.ShouldNotHaveAnyValidationErrors();
        else
        {
            result.ShouldHaveValidationErrorFor(r => r.Environment).WithErrorMessage(expectedMessage!).Only();
        }
    }

    [Theory]
    [InlineData("v1", true, null)]
    [InlineData(null, false, "'Tag' must not be empty.")]
    [InlineData("", false, "'Tag' must not be empty.")]
    [InlineData("   ", false, "'Tag' must not be empty.")]
    public void Tag_ShouldValidate(string? value, bool isValid, string? expectedMessage)
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
    public void AllFieldsNull_ReturnsMultipleErrors()
    {
        var request = new DeployRequest();

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
    }
}
