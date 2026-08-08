using AwesomeAssertions;
using Xunit;
using ApiClient.Extensions;
using ApiClient.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Core.Testing.Extensions;
using Api.Models;
using Core.Testing.Validators;

namespace IntegrationTests.Tests.ApiClient
{
    public class ApiClientTests : Test
    {
        [Fact]
        public async Task MappingEntityFromInvalidResponse_ApiExceptionIsThrownWithExpectedProblemDetails()
        {
            //When
            var response = await ApiClient.Test.ThrowInternalServerError();

            //Then
            var problemDetails = await response.To<ProblemDetails>();
            TraceIdValidator.IsValid(problemDetails.TraceId!).Should().BeTrue();
            ExceptionValidator.IsValid(problemDetails.Exception!).Should().BeTrue();

            var expectedMessage = $$"""
                {
                  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                  "title": "Exception",
                  "status": 500,
                  "detail": "Sensitive data",
                  "instance": "/Test/ThrowInternalServerError",
                  "exception": *,
                  "traceId": "{{problemDetails.TraceId}}"
                }
                """;

            var func = response.To<DeployRequest>;
            await func.Should().ThrowAsync<ApiException>().WithMessage(expectedMessage);
        }

        [Fact]
        public async Task NoContent_ApiClientExceptionIsThrown()
        {
            //When
            var response = await ApiClient.Test.GetOk();

            //Then
            var func = response.To<DeployRequest>;
            await func.Should().ThrowAsync<ApiClientException>().WithMessage("Response content is null, empty or whitespace.");
        }
    }
}
