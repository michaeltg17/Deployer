//using System.Linq.Expressions;
//using Api.Models;
//using Api.Settings;
//using Api.Validation;
//using AwesomeAssertions;
//using Core.Testing.Builders;
//using FluentValidation.TestHelper;
//using Xunit;

//namespace UnitTests.Api.Settings;

//public sealed class DeployRequestValidatorTests : IDisposable
//{
//    readonly string tempDir;
//    readonly DeployRequestValidator validator;

//    public DeployRequestValidatorTests()
//    {
//        tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
//        Directory.CreateDirectory(tempDir);

//        var validProjectDir = Path.Combine(tempDir, "test-project");
//        Directory.CreateDirectory(validProjectDir);
//        File.WriteAllText(Path.Combine(validProjectDir, "docker-compose.yml"), "");

//        var settings = new DeployerSettings
//        {
//            KeePassDbPath = "secrets.kdbx",
//            KeePassDbPassword = "password",
//            ThrowIfNoSecrets = true,
//            ProjectsDir = tempDir
//        };

//        validator = new DeployRequestValidator(settings);
//    }

//    public void Dispose() => Directory.Delete(tempDir, true);

//    public static TheoryData<DeployRequest, Expression<Func<DeployRequest, object?>>, string> GetInvalidPropertyCases()
//    {
//        return new TheoryData<DeployRequest, Expression<Func<DeployRequest, object?>>, string>
//        {
//            // Invalid: project null
//            { new DeployRequestBuilder().WithValues(r => r.Project = null).Build(), r => r.Project, "'Project' must not be empty." },
//            // Invalid: project empty
//            { new DeployRequestBuilder().WithValues(r => r.Project = "").Build(), r => r.Project, "'Project' must not be empty." },
//            // Invalid: project whitespace
//            { new DeployRequestBuilder().WithValues(r => r.Project = " ").Build(), r => r.Project, "'Project' must not be empty." },
//            // Invalid: project missing compose file
//            { new DeployRequestBuilder().WithValues(r => r.Project = "nonexistent").Build(), r => r.Project, "Docker compose file not found for project 'nonexistent': docker-compose.yml" },
//            // Invalid: environment null
//            { new DeployRequestBuilder().WithValues(r => r.Environment = null).Build(), r => r.Environment, "'Environment' must not be empty." },
//            // Invalid: environment empty
//            { new DeployRequestBuilder().WithValues(r => r.Environment = "").Build(), r => r.Environment, "'Environment' must not be empty." },
//            // Invalid: environment whitespace
//            { new DeployRequestBuilder().WithValues(r => r.Environment = "   ").Build(), r => r.Environment, "'Environment' must not be empty." },
//            // Invalid: tag null
//            { new DeployRequestBuilder().WithValues(r => r.Tag = null).Build(), r => r.Tag, "'Tag' must not be empty." },
//            // Invalid: tag empty
//            { new DeployRequestBuilder().WithValues(r => r.Tag = "").Build(), r => r.Tag, "'Tag' must not be empty." },
//            // Invalid: tag whitespace
//            { new DeployRequestBuilder().WithValues(r => r.Tag = "   ").Build(), r => r.Tag, "'Tag' must not be empty." },
//        };
//    }
//    [Theory]
//    [MemberData(nameof(GetInvalidPropertyCases))]
//    public void InvalidProperty_ExpectedMessage(
//        DeployRequest request,
//        Expression<Func<DeployRequest, object>> property,
//        string expectedMessage)
//    {
//        //When
//        var result = validator.TestValidate(request);

//        //Then
//        result.ShouldHaveValidationErrorFor(property).WithErrorMessage(expectedMessage).Only();
//    }

//    [Fact]
//    public void Valid_NoErrors()
//    {
//        //When
//        var result = validator.TestValidate(new DeployRequestBuilder().Build());

//        //Then
//        result.ShouldNotHaveAnyValidationErrors();
//    }

//    [Fact]
//    public void AllNull_ReturnsMultipleErrors()
//    {
//        //When
//        var result = validator.TestValidate(new DeployRequest());

//        //Then
//        result.IsValid.Should().BeFalse();
//        result.Errors.Should().HaveCount(3);
//    }
//}
