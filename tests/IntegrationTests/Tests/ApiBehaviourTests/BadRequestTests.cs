using AwesomeAssertions;
using Xunit;
using Core.Testing.Builders;
using Core.Testing.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using ApiClient.Extensions;
using Core.Testing.Validators;
using static IntegrationTests.Tests.ApiBehaviourTests.BadRequestTests;
using Core.Testing.Serializers;
using Xunit.Sdk;

[assembly: RegisterXunitSerializer(typeof(TestCaseSerializer), typeof(BadRequestCase))]

namespace IntegrationTests.Tests.ApiBehaviourTests
{
    public class BadRequestTests : Test
    {
        public class BadRequestCase
        {
            public object Id;
            public object? Date;
            public object? Request;
            public string ExpectedInstance;
            public string ExpectedDetail;
        }

        public static readonly TheoryDataRow<BadRequestCase>[] TestCases =
        [
            new(new BadRequestCase
            {
                Id = "a",
                Date = null,
                Request = null,
                ExpectedInstance = "/Test/Post/a",
                ExpectedDetail = "Failed to bind parameter \"long id\" from \"a\"."
            }) { TestDisplayName = "Invalid route parameter" },
            new(new BadRequestCase
            {
                Id = (long)1,
                Date = "b",
                Request = null!,
                ExpectedInstance = "/Test/Post/1",
                ExpectedDetail = "Failed to bind parameter \"DateTime date\" from \"b\"."
            }) { TestDisplayName = "Invalid query string parameter" },
            new(new BadRequestCase
            {
                Id = (long)1,
                Date = "2020-01-01",
                Request = null,
                ExpectedInstance = "/Test/Post/1",
                ExpectedDetail = "Required parameter \"PostRequest request\" was not provided from body."
            }) { TestDisplayName = "Missing body" },
            new(new BadRequestCase
            {
                Id = (long)1,
                Date = "2020-01-01",
                Request = "x",
                ExpectedInstance = "/Test/Post/1",
                ExpectedDetail = "Failed to read parameter \"PostRequest request\" from the request body as JSON. The JSON value could not be converted to Api.Endpoints.TestEndpoints+PostRequest. Path: $ | LineNumber: 0 | BytePositionInLine: 3."
            }) { TestDisplayName = "Invalid body" },
            new(new BadRequestCase
            {
                Id = (long)1,
                Date = "2020-01-01",
                Request = new Dictionary<string, object?> { ["id2"] = "notanumber" },
                ExpectedInstance = "/Test/Post/1",
                ExpectedDetail = "Failed to read parameter \"PostRequest request\" from the request body as JSON. The JSON value could not be converted to Api.Endpoints.TestEndpoints+PostRequest. Path: $.id2 | LineNumber: 0 | BytePositionInLine: 19. Either the JSON value is not in a supported format, or is out of bounds for an Int64."
            }) { TestDisplayName = "Invalid body property" }
        ];

        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task Cases(BadRequestCase @case)
        {
            ArgumentNullException.ThrowIfNull(@case);

            //When
            var response = await ApiClient.Test.Post(@case.Id, @case.Date, @case.Request);

            //Then
            var problemDetails = await response.To<ProblemDetails>();
            TraceIdValidator.IsValid(problemDetails.TraceId!).Should().BeTrue();

            var expected = new ProblemDetailsBuilder()
                .WithTraceId(problemDetails.TraceId!)
                .WithBadHttpRequestException()
                .WithInstance(@case.ExpectedInstance)
                .WithDetail(@case.ExpectedDetail)
                .Build();

            problemDetails.Should().BeEquivalentTo(expected);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}