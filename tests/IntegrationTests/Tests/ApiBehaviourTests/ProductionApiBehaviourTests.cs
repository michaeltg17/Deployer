using ApiClient.Extensions;
using AwesomeAssertions;
using Core.Testing.Builders;
using Core.Testing.Extensions;
using Core.Testing.Validators;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Xunit;

namespace IntegrationTests.Tests.ApiBehaviourTests
{
    //[Collection(nameof(ProductionApiCollectionFixture))]
    public class ProductionApiBehaviourTests : Test
    {
        [Fact(Skip = "Sometimes fails. Pending fix.")]
        public async Task InternalServerError_HidesSensitiveData()
        {
            //When
            var response = await ApiClient.Test.ThrowInternalServerError();

            //Then
            var problemDetails = await response.To<ProblemDetails>();
            TraceIdValidator.IsValid(problemDetails.TraceId!).Should().BeTrue();

            var expected = new ProblemDetailsBuilder()
                .WithTraceId(problemDetails.TraceId!)
                .WithHiddenInternalServerError("/Test/ThrowInternalServerError")
                .Build();

            problemDetails.Should().BeEquivalentTo(expected);
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }
    }
}