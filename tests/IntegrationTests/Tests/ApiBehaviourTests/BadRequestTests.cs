using AwesomeAssertions;
using System.Collections.Generic;
using Xunit;
using Core.Testing;
using Core.Testing.Builders;
using Core.Testing.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using ApiClient.Extensions;

namespace IntegrationTests.Tests.ApiBehaviourTests
{
    public class BadRequestTests : Test
    {
        public class BadRequestCase
        {
            public object Id;
            public object Date;
            public object? Request;
            public string ExpectedInstance;
            public string ExpectedDetail;
        }

        public static TheoryData<BadRequestCase> BadRequestCases()
        {
            return new TheoryData<BadRequestCase>
            {
                // Invalid: route parameter id cannot be parsed as long
                new BadRequestCase
                {
                    Id = "a", Date = null!, Request = null!,
                    ExpectedInstance = "/Test/Post/a",
                    ExpectedDetail = "Failed to bind parameter \"long id\" from \"a\"."
                },
                // Invalid: query string date cannot be parsed as DateTime
                new BadRequestCase
                {
                    Id = (long)1, Date = "b", Request = null!,
                    ExpectedInstance = "/Test/Post/1",
                    ExpectedDetail = "Failed to bind parameter \"DateTime date\" from \"b\"."
                },
                // Missing: body not provided
                new BadRequestCase
                {
                    Id = (long)1, Date = "2020-01-01", Request = null!,
                    ExpectedInstance = "/Test/Post/1",
                    ExpectedDetail = "Required parameter \"PostRequest request\" was not provided from body."
                },
                // Invalid: body cannot be converted to expected type
                new BadRequestCase
                {
                    Id = (long)1, Date = "2020-01-01", Request = "x",
                    ExpectedInstance = "/Test/Post/1",
                    ExpectedDetail = "Failed to read parameter \"PostRequest request\" from the request body as JSON. The JSON value could not be converted to Api.Endpoints.TestEndpoints+PostRequest. Path: $ | LineNumber: 0 | BytePositionInLine: 3."
                },
                // Invalid: body property value cannot be converted to expected type
                new BadRequestCase
                {
                    Id = (long)1, Date = "2020-01-01",
                    Request = new Dictionary<string, object?> { ["id2"] = "notanumber" },
                    ExpectedInstance = "/Test/Post/1",
                    ExpectedDetail = "Failed to read parameter \"PostRequest request\" from the request body as JSON. The JSON value could not be converted to System.Int64. Path: $.id2 | LineNumber: 0 | BytePositionInLine: 19."
                }
            };
        }

        [Theory]
        [MemberData(nameof(BadRequestCases))]
        public async Task BadRequest_ExpectedProblemDetails(BadRequestCase testCase)
        {
            //When
            var response = await ApiClient.Test.Post(testCase.Id, testCase.Date, testCase.Request);

            //Then
            var problemDetails = await response.To<ProblemDetails>();
            TraceIdAssertions.Assert(problemDetails.TraceId!).Should().BeTrue();

            var expected = new ProblemDetailsBuilder()
                .WithTraceId(problemDetails.TraceId!)
                .WithBadHttpRequestException()
                .WithInstance(testCase.ExpectedInstance)
                .WithDetail(testCase.ExpectedDetail)
                .Build();

            problemDetails.Should().BeEquivalentTo(expected);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}