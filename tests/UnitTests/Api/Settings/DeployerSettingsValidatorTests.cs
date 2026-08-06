using Api.Settings;
using Api.Validation;
using AwesomeAssertions;
using Xunit;

namespace UnitTests.Api.Settings;

public sealed class DeployerSettingsValidatorTests
{
    readonly DeployerSettingsValidator _validator;

    public DeployerSettingsValidatorTests()
    {
        _validator = new DeployerSettingsValidator();
    }

    [Fact]
    public void Options_NULL_ThrowsArgumentNullException()
    {
        Action action = () => _validator.Validate(null, null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void KeePassDbPath_EMPTY_Fails()
    {
        var settings = new DeployerSettings
        {
            KeePassDbPath = "",
            KeePassDbPassword = "password",
            ThrowIfNoSecrets = true,
            ProjectsDir = "/projects"
        };

        var result = _validator.Validate(null, settings);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle().Which.Should().Contain(nameof(settings.KeePassDbPath));
    }

    [Fact]
    public void KeePassDbPath_WhiteSpace_Fails()
    {
        var settings = new DeployerSettings
        {
            KeePassDbPath = "   ",
            KeePassDbPassword = "password",
            ThrowIfNoSecrets = true,
            ProjectsDir = "/projects"
        };

        var result = _validator.Validate(null, settings);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle().Which.Should().Contain(nameof(settings.KeePassDbPath));
    }

    [Fact]
    public void KeePassDbPassword_EMPTY_Fails()
    {
        var settings = new DeployerSettings
        {
            KeePassDbPath = "secrets.kdbx",
            KeePassDbPassword = "",
            ThrowIfNoSecrets = true,
            ProjectsDir = "/projects"
        };

        var result = _validator.Validate(null, settings);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle().Which.Should().Contain(nameof(settings.KeePassDbPassword));
    }

    [Fact]
    public void KeePassDbPassword_WhiteSpace_Fails()
    {
        var settings = new DeployerSettings
        {
            KeePassDbPath = "secrets.kdbx",
            KeePassDbPassword = "   ",
            ThrowIfNoSecrets = true,
            ProjectsDir = "/projects"
        };

        var result = _validator.Validate(null, settings);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle().Which.Should().Contain(nameof(settings.KeePassDbPassword));
    }

    [Fact]
    public void BothMissing_FailsWithTwoErrors()
    {
        var settings = new DeployerSettings
        {
            KeePassDbPath = "",
            KeePassDbPassword = "",
            ThrowIfNoSecrets = true,
            ProjectsDir = "/projects"
        };

        var result = _validator.Validate(null, settings);

        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCount(2);
        result.Failures.Should().Contain(f => f.Contains(nameof(settings.KeePassDbPath)));
        result.Failures.Should().Contain(f => f.Contains(nameof(settings.KeePassDbPassword)));
    }

    [Fact]
    public void ValidSettings_Succeeds()
    {
        var settings = new DeployerSettings
        {
            KeePassDbPath = "secrets.kdbx",
            KeePassDbPassword = "password",
            ThrowIfNoSecrets = true,
            ProjectsDir = "/projects"
        };

        var result = _validator.Validate(null, settings);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void ValidSettings_NameParameterIgnored()
    {
        var settings = new DeployerSettings
        {
            KeePassDbPath = "secrets.kdbx",
            KeePassDbPassword = "password",
            ThrowIfNoSecrets = true,
            ProjectsDir = "/projects"
        };

        var result = _validator.Validate("some-name", settings);

        result.Succeeded.Should().BeTrue();
    }
}
