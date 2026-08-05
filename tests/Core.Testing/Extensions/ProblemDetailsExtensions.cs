using Microsoft.AspNetCore.Mvc;

namespace Core.Testing.Extensions
{
    public static class ProblemDetailsExtensions
    {
        extension(ProblemDetails problemDetails)
        {
            public string? TraceId => problemDetails.Extensions["traceId"] as string;
            public string? Exception => problemDetails.Extensions["exception"] as string;
        }
    }
}
