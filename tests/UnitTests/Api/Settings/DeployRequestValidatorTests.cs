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

    public sealed class TestCase
    {
        public DeployRequest Request { get; init; }
        public (string Property, string Message)[] ExpectedErrors { get; init; }
        public bool IsValid => ExpectedErrors.Length == 0;
    }

    public static readonly TheoryDataRow<TestCase>[] TestCases =
    [
        new(new TestCase { Request = new DeployRequestBuilder().Build() }) { TestDisplayName = "Valid" },
        new(new TestCase { Request = new DeployRequestBuilder().WithValues(r => r.Project = null).Build(), ExpectedErrors = [(nameof(DeployRequest.Project), "'Project' must not be empty.")] }) { TestDisplayName = "Invalid: Project null" },
        new(new TestCase { Request = new DeployRequestBuilder().WithValues(r => r.Project = "").Build(), ExpectedErrors = [(nameof(DeployRequest.Project), "'Project' must not be empty.")] }) { TestDisplayName = "Invalid: Project empty" },
        new(new TestCase { Request = new DeployRequestBuilder().WithValues(r => r.Project = " ").Build(), ExpectedErrors = [(nameof(DeployRequest.Project), "'Project' must not be empty.")] }) { TestDisplayName = "Invalid: Project whitespace" },
        new(new TestCase { Request = new DeployRequestBuilder().WithValues(r => r.Project = "nonexistent").Build(), ExpectedErrors = [(nameof(DeployRequest.Project), "Docker compose file not found for project 'nonexistent': docker-compose.yml")] }) { TestDisplayName = "Invalid: Missing compose file" },
        new(new TestCase { Request = new DeployRequestBuilder().WithValues(r => r.Environment = null).Build(), ExpectedErrors = [(nameof(DeployRequest.Environment), "'Environment' must not be empty.")] }) { TestDisplayName = "Invalid: Environment null" },
        new(new TestCase { Request = new DeployRequestBuilder().WithValues(r => r.Environment = "").Build(), ExpectedErrors = [(nameof(DeployRequest.Environment), "'Environment' must not be empty.")] }) { TestDisplayName = "Invalid: Environment empty" },
        new(new TestCase { Request = new DeployRequestBuilder().WithValues(r => r.Environment = "   ").Build(), ExpectedErrors = [(nameof(DeployRequest.Environment), "'Environment' must not be empty.")] }) { TestDisplayName = "Invalid: Environment whitespace" },
        new(new TestCase { Request = new DeployRequestBuilder().WithValues(r => r.Tag = null).Build(), ExpectedErrors = [(nameof(DeployRequest.Tag), "'Tag' must not be empty.")] }) { TestDisplayName = "Invalid: Tag null" },
        new(new TestCase { Request = new DeployRequestBuilder().WithValues(r => r.Tag = "").Build(), ExpectedErrors = [(nameof(DeployRequest.Tag), "'Tag' must not be empty.")] }) { TestDisplayName = "Invalid: Tag empty" },
        new(new TestCase { Request = new DeployRequestBuilder().WithValues(r => r.Tag = "   ").Build(), ExpectedErrors = [(nameof(DeployRequest.Tag), "'Tag' must not be empty.")] }) { TestDisplayName = "Invalid: Tag whitespace" },
        new(new TestCase { Request = new DeployRequest(), ExpectedErrors = [(nameof(DeployRequest.Project), "'Project' must not be empty."), (nameof(DeployRequest.Environment), "'Environment' must not be empty."), (nameof(DeployRequest.Tag), "'Tag' must not be empty.")] }) { TestDisplayName = "AllNull: Multiple errors" },
    ];

    [Theory]
    [MemberData(nameof(TestCases))]
    public void Cases(TestCase @case)
    {
        //When
        var result = validator.TestValidate(@case.Request);

        //Then
        if (@case.IsValid)
            result.ShouldNotHaveAnyValidationErrors();
        else
        {
            var errors = @case.ExpectedErrors;
            result.Errors.Should().HaveCount(errors.Length);
            foreach (var (property, message) in errors)
            {
                var assertion = result.ShouldHaveValidationErrorFor(property).WithErrorMessage(message);
                if (errors.Length == 1)
                    assertion.Only();
            }
        }
    }
}
