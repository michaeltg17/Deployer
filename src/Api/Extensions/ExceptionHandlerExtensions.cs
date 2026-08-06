using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
namespace Api.Extensions
{
    internal static class ExceptionHandlerExtensions
    {
        public static WebApplication UseExceptionHandler(this WebApplication app)
        {
            app.UseExceptionHandler(config => config.Run(async httpContext =>
            {
                httpContext.Response.ContentType = "application/problem+json";
                var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();

                var exceptionHandlerFeature = httpContext.Features.GetRequiredFeature<IExceptionHandlerFeature>();
                var exception = exceptionHandlerFeature.Error;

                httpContext.Response.StatusCode = exception switch
                {
                    BadHttpRequestException => (int)HttpStatusCode.BadRequest,
                    ValidationException => (int)HttpStatusCode.BadRequest,
                    _ => (int)HttpStatusCode.InternalServerError,
                };

                await problemDetailsService.WriteAsync(BuildProblemDetailsContext(exception, httpContext));
            }));

            return app;
        }

        static ProblemDetailsContext BuildProblemDetailsContext(Exception exception, HttpContext httpContext)
        {
            var isInternalServerError = httpContext.Response.StatusCode == (int)HttpStatusCode.InternalServerError;
            var isValidationException = exception is ValidationException;
            var isDevelopment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment();

            var detail = exception switch
            {
                BadHttpRequestException { InnerException: JsonException jsonEx } => exception.Message + " " + jsonEx.Message,
                BadHttpRequestException => exception.Message,
                _ when isInternalServerError && !isDevelopment => "Internal server error. Please contact the API support.",
                _ when isValidationException => "One or more validation errors occurred.",
                _ => exception.Message
            };

            var typeUri = httpContext.Response.StatusCode switch
            {
                (int)HttpStatusCode.BadRequest => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                (int)HttpStatusCode.NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            };

            var problemDetails = new ProblemDetails
            {
                Type = typeUri,
                Title = isInternalServerError && !isDevelopment ? "InternalServerError" : exception.GetType().GetNameWithoutGenericArity(),
                Detail = detail,
                Status = httpContext.Response.StatusCode,
                Instance = httpContext.Request.Path,
                Extensions = new Dictionary<string, object?>()
            };

            if (exception is ValidationException validationException)
                problemDetails.Extensions["errors"] = validationException.Errors.ToValidationProblemErrors();

            if (isInternalServerError && isDevelopment)
                problemDetails.Extensions["exception"] = exception.ToString();

            return new ProblemDetailsContext
            {
                Exception = isInternalServerError && !isDevelopment ? null : exception,
                HttpContext = httpContext,
                ProblemDetails = problemDetails
            };
        }
    }
}
