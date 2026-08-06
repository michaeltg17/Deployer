using ApiClient.Extensions;
using Core.Testing.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Core.Testing.Builders;
using AwesomeAssertions;

namespace Core.Testing.Validators
{
    public static class ProblemDetailsValidator
    {
        public static async Task ValidateNotAllFoundException(
            HttpResponseMessage response, string entity, string baseInstance, long[] ids)
        {
            var builder = new ProblemDetailsBuilder().WithNotAllFoundException(entity, baseInstance, ids);
            await ValidateCommon(response, builder, HttpStatusCode.NotFound);
        }

        public static async Task ValidateNotFoundException(
            HttpResponseMessage response, string entity, string baseInstance, long id)
        {
            var builder = new ProblemDetailsBuilder().WithNotFoundException(entity, baseInstance, id);
            await ValidateCommon(response, builder, HttpStatusCode.NotFound);
        }

        public static async Task ValidateValidationException(
            HttpResponseMessage response,
            string instance,
            IDictionary<string, string[]> expectedErrors)
        {
            var builder = new ProblemDetailsBuilder().WithValidationException(instance, expectedErrors);
            await ValidateCommon(response, builder, HttpStatusCode.BadRequest);
        }

        static async Task ValidateCommon(
            HttpResponseMessage response,
            ProblemDetailsBuilder builder,
            HttpStatusCode statusCode)
        {
            var problemDetails = await response.To<ProblemDetails>();
            TraceIdValidator.IsValid(problemDetails.TraceId!).Should().BeTrue();

            var expected = builder
                .WithTraceId(problemDetails.TraceId!)
                .Build();

            problemDetails.Should().BeEquivalentTo(expected);
            response.StatusCode.Should().Be(statusCode);
        }
    }
}
