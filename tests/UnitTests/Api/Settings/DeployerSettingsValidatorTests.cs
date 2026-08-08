using Api.Settings;
using Api.Validation;
using AwesomeAssertions;
using Core.Testing.Builders;
using Core.Testing.Serializers;
using Xunit;
using Xunit.Sdk;
using static UnitTests.Api.Settings.DeployerSettingsValidatorTests;

[assembly: RegisterXunitSerializer(typeof(TestCaseSerializer), typeof(TestCase))]

namespace UnitTests.Api.Settings;

public sealed class DeployerSettingsValidatorTests
{
    readonly DeployerSettingsValidator validator;

    public DeployerSettingsValidatorTests()
    {
        validator = new DeployerSettingsValidator();
    }

    public sealed class TestCase
    {
        public DeployerSettings? Settings { get; init; }
        public string[] ExpectedErrors { get; init; } = [];
        public bool IsValid => ExpectedErrors.Length == 0;
    }

    public static readonly TheoryDataRow<TestCase>[] TestCases =
    [
        new(new TestCase
        {
            Settings = new DeployerSettingsBuilder().Build()
        }) { TestDisplayName = "Valid" },
        new(new TestCase
        {
            Settings = new DeployerSettingsBuilder().WithValues(s => s.KeePassDbPath = null!).Build(),
            ExpectedErrors = ["The 'KeePassDbPath' setting is required"]
        }) { TestDisplayName = "Invalid: KeePassDbPath null" },
        new(new TestCase
        {
            Settings = new DeployerSettingsBuilder().WithValues(s => s.KeePassDbPath = "").Build(),
            ExpectedErrors = ["The 'KeePassDbPath' setting is required"]
        }) { TestDisplayName = "Invalid: KeePassDbPath empty" },
        new(new TestCase
        {
            Settings = new DeployerSettingsBuilder().WithValues(s => s.KeePassDbPath = "   ").Build(),
            ExpectedErrors = ["The 'KeePassDbPath' setting is required"]
        }) { TestDisplayName = "Invalid: KeePassDbPath whitespace" },
        new(new TestCase
        {
            Settings = new DeployerSettingsBuilder().WithValues(s => s.KeePassDbPassword = null!).Build(),
            ExpectedErrors = ["The 'KeePassDbPassword' setting is required"]
        }) { TestDisplayName = "Invalid: KeePassDbPassword null" },
        new(new TestCase
        {
            Settings = new DeployerSettingsBuilder().WithValues(s => s.KeePassDbPassword = "").Build(),
            ExpectedErrors = ["The 'KeePassDbPassword' setting is required"]
        }) { TestDisplayName = "Invalid: KeePassDbPassword empty" },
        new(new TestCase
        {
            Settings = new DeployerSettingsBuilder().WithValues(s => s.KeePassDbPassword = "   ").Build(),
            ExpectedErrors = ["The 'KeePassDbPassword' setting is required"]
        }) { TestDisplayName = "Invalid: KeePassDbPassword whitespace" },
        new(new TestCase
        {
            Settings = new DeployerSettingsBuilder().WithValues(s =>
            {
                s.KeePassDbPath = null!;
                s.KeePassDbPassword = null!;
            }).Build(),
            ExpectedErrors = [
                "The 'KeePassDbPath' setting is required",
                "The 'KeePassDbPassword' setting is required"
            ]
        }) { TestDisplayName = "Invalid: all null multiple errors" },
        new(new TestCase
        {
            Settings = null!,
            ExpectedErrors = ["DeployerSettings must not be null"]
        }) { TestDisplayName = "Invalid: null options" },
    ];

    [Theory]
    [MemberData(nameof(TestCases))]
    public void Cases(TestCase @case)
    {
        ArgumentNullException.ThrowIfNull(@case);

        //When
        var result = validator.Validate(null, @case.Settings!);

        //Then
        if (@case.IsValid)
            result.Succeeded.Should().BeTrue();
        else
        {
            var errors = @case.ExpectedErrors;
            result.Failed.Should().BeTrue();
            result.Failures.Should().HaveCount(errors.Length);
            foreach (var message in errors)
                result.Failures.Should().Contain(message);
        }
    }
}
