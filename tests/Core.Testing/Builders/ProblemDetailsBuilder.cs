using Api.Builders;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Core.Testing.Builders
{
    public class ProblemDetailsBuilder : BuilderWithInstance<ProblemDetailsBuilder, ProblemDetails>
    {
        public ProblemDetailsBuilder() { Item.Extensions = new Dictionary<string, object?>(); }

        public ProblemDetailsBuilder WithTraceId(string traceId)
        {
            Item.Extensions.Add("traceId", traceId);
            return this;
        }

        public ProblemDetailsBuilder WithNotFound()
        {
            Item.Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5";
            Item.Title = "Not Found";
            Item.Status = (int)HttpStatusCode.NotFound;

            return this;
        }

        public ProblemDetailsBuilder WithNotAllFoundException(string entity, string baseInstance, long[] ids)
        {
            Item.Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5";
            Item.Title = "NotAllFoundException";
            Item.Status = (int)HttpStatusCode.NotFound;
            Item.Detail = $"The following ids '{string.Join(", ", ids)}' were not found for entity '{entity}'.";
            Item.Instance = baseInstance;
            Item.Extensions["notFoundIds"] = ids;

            return this;
        }

        public ProblemDetailsBuilder WithNotFoundException(string entity, string baseInstance, long id)
        {
            Item.Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5";
            Item.Title = "NotFoundException";
            Item.Status = (int)HttpStatusCode.NotFound;
            Item.Detail = $"{entity} with id '{id}' was not found.";
            Item.Instance = $"{baseInstance}/{id}";

            return this;
        }

        public ProblemDetailsBuilder WithBadHttpRequestException()
        {
            Item.Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1";
            Item.Title = "BadHttpRequestException";
            Item.Status = (int)HttpStatusCode.BadRequest;

            return this;
        }

        public ProblemDetailsBuilder WithInstance(string instance)
        {
            Item.Instance = instance;
            return this;
        }

        public ProblemDetailsBuilder WithDetail(string detail)
        {
            Item.Detail = detail;
            return this;
        }

        public ProblemDetailsBuilder WithHiddenInternalServerError(string instance)
        {
            Item.Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1";
            Item.Title = "InternalServerError";
            Item.Status = (int)HttpStatusCode.InternalServerError;
            Item.Detail = "Internal server error. Please contact the API support.";
            Item.Instance = instance;

            return this;
        }

        public ProblemDetailsBuilder WithInternalServerError(string title, string detail, string instance)
        {
            Item.Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1";
            Item.Title = title;
            Item.Status = (int)HttpStatusCode.InternalServerError;
            Item.Detail = detail;
            Item.Instance = instance;

            return this;
        }

        public ProblemDetailsBuilder WithException(string exceptionText)
        {
            Item.Extensions["exception"] = exceptionText;
            return this;
        }

        public ProblemDetailsBuilder WithValidationException(string instance, IDictionary<string, string[]> errors)
        {
            Item.Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1";
            Item.Title = "ValidationException";
            Item.Status = (int)HttpStatusCode.BadRequest;
            Item.Detail = "One or more validation errors occurred.";
            Item.Instance = instance;
            Item.Extensions["errors"] = errors;

            return this;
        }
    }
}
