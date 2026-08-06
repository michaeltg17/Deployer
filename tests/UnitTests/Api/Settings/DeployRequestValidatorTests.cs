using Api.Models;
using Api.Settings;
using Api.Validation;
using AwesomeAssertions;
using Xunit;

namespace UnitTests.Api.Settings;

public sealed class DeployRequestValidatorTests : IDisposable
{
    readonly string _tempDir;
    readonly DeployRequestValidator _validator;

    public DeployRequestValidatorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        var validProjectDir = Path.Combine(_tempDir, "valid-project");
        Directory.CreateDirectory(validProjectDir);
        File.WriteAllText(Path.Combine(validProjectDir, "docker-compose.yml"), "");

        var settings = new DeployerSettings
        {
            KeePassDbPath = "secrets.kdbx",
            KeePassDbPassword = "password",
            ThrowIfNoSecrets = true,
            ProjectsDir = _tempDir
        };

        _validator = new DeployRequestValidator(settings);
    }

    public void Dispose()
    {
        Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void Project_NULL_Fails()
    {
        var request = new DeployRequest { Project = null, Environment = "dev", Tag = "v1" };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Project");
    }

    [Fact]
    public void Project_Empty_Fails()
    {
        var request = new DeployRequest { Project = "", Environment = "dev", Tag = "v1" };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Project");
    }

    [Fact]
    public void Project_WhiteSpace_Fails()
    {
        var request = new DeployRequest { Project = " ", Environment = "dev", Tag = "v1" };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Project");
    }

    [Fact]
    public void Project_MissingComposeFile_Fails()
    {
        var request = new DeployRequest { Project = "nonexistent", Environment = "dev", Tag = "v1" };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "Project" && e.ErrorMessage.Contains("docker-compose.yml"));
    }

    [Fact]
    public void Project_ComposeFileExists_Passes()
    {
        var request = new DeployRequest { Project = "valid-project", Environment = "dev", Tag = "v1" };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Environment_NULL_Fails()
    {
        var request = new DeployRequest { Project = "valid-project", Environment = null, Tag = "v1" };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Environment");
    }

    [Fact]
    public void Environment_Empty_Fails()
    {
        var request = new DeployRequest { Project = "valid-project", Environment = "", Tag = "v1" };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Environment");
    }

    [Fact]
    public void Environment_WhiteSpace_Fails()
    {
        var request = new DeployRequest { Project = "valid-project", Environment = "   ", Tag = "v1" };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Environment");
    }

    [Fact]
    public void Tag_NULL_Fails()
    {
        var request = new DeployRequest { Project = "valid-project", Environment = "dev", Tag = null };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Tag");
    }

    [Fact]
    public void Tag_Empty_Fails()
    {
        var request = new DeployRequest { Project = "valid-project", Environment = "dev", Tag = "" };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Tag");
    }

    [Fact]
    public void Tag_WhiteSpace_Fails()
    {
        var request = new DeployRequest { Project = "valid-project", Environment = "dev", Tag = "   " };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Tag");
    }

    [Fact]
    public void AllValid_Passes()
    {
        var request = new DeployRequest { Project = "valid-project", Environment = "dev", Tag = "v1" };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AllFieldsNull_ReturnsMultipleErrors()
    {
        var request = new DeployRequest();
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
    }
}
