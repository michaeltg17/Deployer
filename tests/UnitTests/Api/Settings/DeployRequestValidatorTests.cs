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

    public sealed record InvalidTestCase(string Property, object? Value, string ExpectedMessage)
    {
        public string TestDisplayName { get; init } = $"Invalid{Property} - {(Value == null ? "null" : Value)}";
    }

    public static readonly InvalidTestCase[] InvalidCases =
    [
        new(nameof(DeployRequest.Project), null, "'Project' must not be empty."),
        new(nameof(DeployRequest.Project), "", "'Project' must not be empty."),
        new(nameof(DeployRequest.Project), " ", "'Project' must not be empty."),
        new(nameof(DeployRequest.Project), "nonexistent", "Docker compose file not found for project 'nonexistent': docker-compose.yml"),
        new(nameof(DeployRequest.Environment), null, "'Environment' must not be empty."),
        new(nameof(DeployRequest.Environment), "", "'Environment' must not be empty."),
        new(nameof(DeployRequest.Environment), "   ", "'Environment' must not be empty."),
        new(nameof(DeployRequest.Tag), null, "'Tag' must not be empty."),
        new(nameof(DeployRequest.Tag), "", "'Tag' must not be empty."),
        new(nameof(DeployRequest.Tag), "   ", "'Tag' must not be empty."),
    ];

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void InvalidProperty_Error(InvalidTestCase testCase)
    {
        //Given
        var request = new DeployRequestBuilder()
            .WithValue(testCase.Property, testCase.Value)
            .Build();

        //When
        var result = validator.TestValidate(request);

        //Then
        result.ShouldHaveValidationErrorFor(testCase.Property)
            .WithErrorMessage(testCase.ExpectedMessage)
            .Only();
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
