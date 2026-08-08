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

    public static readonly TheoryDataRow<DeployRequest, (string Property, string Message)[]?, bool>[] TestCases =
    [
        new(new DeployRequestBuilder().Build(), null, true) { TestDisplayName = "Valid" },
        new(new DeployRequestBuilder().WithValues(r => r.Project = null).Build(), [(nameof(DeployRequest.Project), "'Project' must not be empty.")], false) { TestDisplayName = "Invalid: Project null" },
        new(new DeployRequestBuilder().WithValues(r => r.Project = "").Build(), [(nameof(DeployRequest.Project), "'Project' must not be empty.")], false) { TestDisplayName = "Invalid: Project empty" },
        new(new DeployRequestBuilder().WithValues(r => r.Project = " ").Build(), [(nameof(DeployRequest.Project), "'Project' must not be empty.")], false) { TestDisplayName = "Invalid: Project whitespace" },
        new(new DeployRequestBuilder().WithValues(r => r.Project = "nonexistent").Build(), [(nameof(DeployRequest.Project), "Docker compose file not found for project 'nonexistent': docker-compose.yml")], false) { TestDisplayName = "Invalid: Missing compose file" },
        new(new DeployRequestBuilder().WithValues(r => r.Environment = null).Build(), [(nameof(DeployRequest.Environment), "'Environment' must not be empty.")], false) { TestDisplayName = "Invalid: Environment null" },
        new(new DeployRequestBuilder().WithValues(r => r.Environment = "").Build(), [(nameof(DeployRequest.Environment), "'Environment' must not be empty.")], false) { TestDisplayName = "Invalid: Environment empty" },
        new(new DeployRequestBuilder().WithValues(r => r.Environment = "   ").Build(), [(nameof(DeployRequest.Environment), "'Environment' must not be empty.")], false) { TestDisplayName = "Invalid: Environment whitespace" },
        new(new DeployRequestBuilder().WithValues(r => r.Tag = null).Build(), [(nameof(DeployRequest.Tag), "'Tag' must not be empty.")], false) { TestDisplayName = "Invalid: Tag null" },
        new(new DeployRequestBuilder().WithValues(r => r.Tag = "").Build(), [(nameof(DeployRequest.Tag), "'Tag' must not be empty.")], false) { TestDisplayName = "Invalid: Tag empty" },
        new(new DeployRequestBuilder().WithValues(r => r.Tag = "   ").Build(), [(nameof(DeployRequest.Tag), "'Tag' must not be empty.")], false) { TestDisplayName = "Invalid: Tag whitespace" },
        new(new DeployRequest(), [(nameof(DeployRequest.Project), "'Project' must not be empty."), (nameof(DeployRequest.Environment), "'Environment' must not be empty."), (nameof(DeployRequest.Tag), "'Tag' must not be empty.")], false) { TestDisplayName = "AllNull: Multiple errors" },
    ];

    [Theory]
    [MemberData(nameof(TestCases))]
    public void Cases(DeployRequest request, (string Property, string Message)[]? expectedErrors, bool isValid)
    {
        //When
        var result = validator.TestValidate(request);

        //Then
        if (isValid)
            result.ShouldNotHaveAnyValidationErrors();
        else
        {
            result.Errors.Should().HaveCount(expectedErrors!.Length);
            foreach (var (property, message) in expectedErrors)
            {
                var assertion = result.ShouldHaveValidationErrorFor(property).WithErrorMessage(message);
                if (expectedErrors.Length == 1)
                    assertion.Only();
            }
        }
    }
}
