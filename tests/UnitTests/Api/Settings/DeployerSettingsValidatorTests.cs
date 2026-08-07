using Api.Validation;
using AwesomeAssertions;
using Core.Testing.Builders;
using Xunit;

namespace UnitTests.Api.Settings;

public sealed class DeployerSettingsValidatorTests
{
    readonly DeployerSettingsValidator validator;

    public DeployerSettingsValidatorTests()
    {
        validator = new DeployerSettingsValidator();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void InvalidKeePassDbPath_Error(string? keePassDbPath)
    {
        //Given
        var settings = new DeployerSettingsBuilder()
            .WithValues(s => s.KeePassDbPath = keePassDbPath!)
            .Build();

        //When
        var result = validator.Validate(null, settings);

        //Then
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle().Which.Should().Be("The 'KeePassDbPath' setting is required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void InvalidKeePassDbPassword_Error(string? keePassDbPassword)
    {
        //Given
        var settings = new DeployerSettingsBuilder()
            .WithValues(s => s.KeePassDbPassword = keePassDbPassword!)
            .Build();

        //When
        var result = validator.Validate(null, settings);

        //Then
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle().Which.Should().Be("The 'KeePassDbPassword' setting is required");
    }

    [Fact]
    public void Valid_NoErrors()
    {
        //When
        var result = validator.Validate(null, new DeployerSettingsBuilder().Build());

        //Then
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void AllNull_MultipleErrors()
    {
        //Given
        var settings = new DeployerSettingsBuilder()
            .WithValues(s =>
            {
                s.KeePassDbPath = null!;
                s.KeePassDbPassword = null!;
            })
            .Build();

        //When
        var result = validator.Validate(null, settings);

        //Then
        result.Failures.Should().HaveCount(2);
        result.Failures.Should().Contain("The 'KeePassDbPath' setting is required");
        result.Failures.Should().Contain("The 'KeePassDbPassword' setting is required");
    }

    [Fact]
    public void NullOptions_ThrowsArgumentNullException()
    {
        Action action = () => validator.Validate(null, null!);
        action.Should().Throw<ArgumentNullException>();
    }
}
