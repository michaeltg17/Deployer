using ApiClient.Converters;
using ApiClient.Exceptions;
using ApiClient.Validators;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ApiClient.Extensions
{
    public static class HttpResponseMessageExtensions
    {
        static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new NestedObjectConverter() }
        };

        public static async Task<T> To<T>(this HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
                throw new ApiClientException("Response content is null, empty or whitespace.");

            if (response.IsSuccessStatusCode)
            {
                var item = JsonSerializer.Deserialize<T>(content, JsonSerializerOptions)!;
                if (typeof(T) == typeof(ProblemDetails))
                {
                    var problemDetails = (item as ProblemDetails)!;
                    ProblemDetailsValidator.EnsureValid(problemDetails);
                    return (T)(object)problemDetails;
                }
                else
                {
                    return item;
                }
            }
            else if (typeof(T) == typeof(ProblemDetails))
            {
                var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(content, JsonSerializerOptions)!;
                ProblemDetailsValidator.EnsureValid(problemDetails);
                return (T)(object)problemDetails;
            }
            else
            {
                var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(content, JsonSerializerOptions)!;
                throw new ApiException(problemDetails);
            }
        }

        public static async Task<T> To<T>(this Task<HttpResponseMessage> responseTask)
        {
            var response = await responseTask;
            return await response.To<T>();
        }
    }
}
