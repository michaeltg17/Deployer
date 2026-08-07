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

    public static TheoryData<DeployRequest, Expression<Func<DeployRequest, object>>, bool> GetProjectCases()
    {
        return new TheoryData<DeployRequest, Expression<Func<DeployRequest, object>>, bool>
        {
            // Valid: compose file exists
            { new DeployRequestBuilder().Build(), r => r.Project, true },
            // Invalid: null
            { new DeployRequestBuilder().WithValues(r => r.Project = null).Build(), r => r.Project, false },
            // Invalid: empty
            { new DeployRequestBuilder().WithValues(r => r.Project = "").Build(), r => r.Project, false },
            // Invalid: whitespace
            { new DeployRequestBuilder().WithValues(r => r.Project = " ").Build(), r => r.Project, false },
            // Invalid: missing compose file
            { new DeployRequestBuilder().WithValues(r => r.Project = "nonexistent").Build(), r => r.Project, false },
        };
    }

    [Theory]
    [MemberData(nameof(GetProjectCases))]
    public void Project_ShouldHaveExpectedResult(
        DeployRequest request,
        Expression<Func<DeployRequest, object>> property,
        bool isValid)
    {
        // When
        var result = validator.TestValidate(request);

        // Then
        if (isValid) result.ShouldNotHaveAnyValidationErrors();
        else result.ShouldHaveValidationErrorFor(property).Only();
    }

    public static TheoryData<DeployRequest, Expression<Func<DeployRequest, object>>, bool> GetEnvironmentCases()
    {
        return new TheoryData<DeployRequest, Expression<Func<DeployRequest, object>>, bool>
        {
            // Valid: non-empty
            { new DeployRequestBuilder().Build(), r => r.Environment, true },
            // Invalid: null
            { new DeployRequestBuilder().WithValues(r => r.Environment = null).Build(), r => r.Environment, false },
            // Invalid: empty
            { new DeployRequestBuilder().WithValues(r => r.Environment = "").Build(), r => r.Environment, false },
            // Invalid: whitespace
            { new DeployRequestBuilder().WithValues(r => r.Environment = "   ").Build(), r => r.Environment, false },
        };
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentCases))]
    public void Environment_ShouldHaveExpectedResult(
        DeployRequest request,
        Expression<Func<DeployRequest, object>> property,
        bool isValid)
    {
        // When
        var result = validator.TestValidate(request);

        // Then
        if (isValid) result.ShouldNotHaveAnyValidationErrors();
        else result.ShouldHaveValidationErrorFor(property).Only();
    }

    public static TheoryData<DeployRequest, Expression<Func<DeployRequest, object>>, bool> GetTagCases()
    {
        return new TheoryData<DeployRequest, Expression<Func<DeployRequest, object>>, bool>
        {
            // Valid: non-empty
            { new DeployRequestBuilder().Build(), r => r.Tag, true },
            // Invalid: null
            { new DeployRequestBuilder().WithValues(r => r.Tag = null).Build(), r => r.Tag, false },
            // Invalid: empty
            { new DeployRequestBuilder().WithValues(r => r.Tag = "").Build(), r => r.Tag, false },
            // Invalid: whitespace
            { new DeployRequestBuilder().WithValues(r => r.Tag = "   ").Build(), r => r.Tag, false },
        };
    }

    [Theory]
    [MemberData(nameof(GetTagCases))]
    public void Tag_ShouldHaveExpectedResult(
        DeployRequest request,
        Expression<Func<DeployRequest, object>> property,
        bool isValid)
    {
        // When
        var result = validator.TestValidate(request);

        // Then
        if (isValid) result.ShouldNotHaveAnyValidationErrors();
        else result.ShouldHaveValidationErrorFor(property).Only();
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
