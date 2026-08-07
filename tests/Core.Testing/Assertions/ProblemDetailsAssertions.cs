using ApiClient.Extensions;
using Core.Testing.Builders;
using Core.Testing.Extensions;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Core.Testing.Assertions
{
    public static class ProblemDetailsAssertions
    {
        public static async Task AssertValidationException(
            HttpResponseMessage response,
            string instance,
            IDictionary<string, string[]> expectedErrors)
        {
            var builder = new ProblemDetailsBuilder().WithValidationException(instance, expectedErrors);
            await Assert(response, builder, HttpStatusCode.BadRequest);
        }

        static async Task Assert(
            HttpResponseMessage response,
            ProblemDetailsBuilder builder,
            HttpStatusCode statusCode)
        {
            var problemDetails = await response.To<ProblemDetails>();
            TraceIdAssertions.Assert(problemDetails.TraceId!).Should().BeTrue();

            var expected = builder
                .WithTraceId(problemDetails.TraceId!)
                .Build();

            problemDetails.Should().BeEquivalentTo(expected);
            response.StatusCode.Should().Be(statusCode);
        }
    }
}
