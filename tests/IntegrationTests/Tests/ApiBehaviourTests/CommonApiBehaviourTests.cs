using Api.Endpoints;
using ApiClient.Extensions;
using AwesomeAssertions;
using Core.Testing;
using Core.Testing.Builders;
using Core.Testing.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Xunit;
using static Api.Endpoints.TestEndpoints;

namespace IntegrationTests.Tests.ApiBehaviourTests
{
    public class CommonApiBehaviourTests : Test
    {
        [Fact]
        public async Task NonexistentRoute_ExpectedProblemDetails()
        {
            //When
            var response = await ApiClient.Test.RequestUnexistingRoute();

            //Then
            var problemDetails = await response.To<ProblemDetails>();
            TraceIdAssertions.Assert(problemDetails.TraceId!).Should().BeTrue();

            var expected = new ProblemDetailsBuilder()
                .WithTraceId(problemDetails.TraceId!)
                .WithNotFound()
                .Build();

            problemDetails.Should().BeEquivalentTo(expected);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ValidRequest_Ok()
        {
            //When
            var response = await ApiClient.Test.Post(1L, new DateTime(2020, 1, 1), new PostRequest { Id2 = 2L });

            //Then
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}