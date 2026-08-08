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

    public static readonly TheoryDataRow<DeployRequest, string, string>[] InvalidCases =
    [
        new(new DeployRequestBuilder().WithValues(r => r.Project = null).Build(), nameof(DeployRequest.Project), "'Project' must not be empty.") { TestDisplayName = "Invalid: Project null" },
        new(new DeployRequestBuilder().WithValues(r => r.Project = "").Build(), nameof(DeployRequest.Project), "'Project' must not be empty.") { TestDisplayName = "Invalid: Project empty" },
        new(new DeployRequestBuilder().WithValues(r => r.Project = " ").Build(), nameof(DeployRequest.Project), "'Project' must not be empty.") { TestDisplayName = "Invalid: Project whitespace" },
        new(new DeployRequestBuilder().WithValues(r => r.Project = "nonexistent").Build(), nameof(DeployRequest.Project), "Docker compose file not found for project 'nonexistent': docker-compose.yml") { TestDisplayName = "Invalid: Missing compose file" },
        new(new DeployRequestBuilder().WithValues(r => r.Environment = null).Build(), nameof(DeployRequest.Environment), "'Environment' must not be empty.") { TestDisplayName = "Invalid: Environment null" },
        new(new DeployRequestBuilder().WithValues(r => r.Environment = "").Build(), nameof(DeployRequest.Environment), "'Environment' must not be empty.") { TestDisplayName = "Invalid: Environment empty" },
        new(new DeployRequestBuilder().WithValues(r => r.Environment = "   ").Build(), nameof(DeployRequest.Environment), "'Environment' must not be empty.") { TestDisplayName = "Invalid: Environment whitespace" },
        new(new DeployRequestBuilder().WithValues(r => r.Tag = null).Build(), nameof(DeployRequest.Tag), "'Tag' must not be empty.") { TestDisplayName = "Invalid: Tag null" },
        new(new DeployRequestBuilder().WithValues(r => r.Tag = "").Build(), nameof(DeployRequest.Tag), "'Tag' must not be empty.") { TestDisplayName = "Invalid: Tag empty" },
        new(new DeployRequestBuilder().WithValues(r => r.Tag = "   ").Build(), nameof(DeployRequest.Tag), "'Tag' must not be empty.") { TestDisplayName = "Invalid: Tag whitespace" },
    ];

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void InvalidProperty_Error(DeployRequest request, string property, string expectedMessage)
    {
        //When
        var result = validator.TestValidate(request);

        //Then
        result.ShouldHaveValidationErrorFor(property)
            .WithErrorMessage(expectedMessage)
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
