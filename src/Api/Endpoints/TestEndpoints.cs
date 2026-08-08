using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace Api.Endpoints
{
    public static class TestEndpoints
    {
        [SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Test")]
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("GetOk", (CancellationToken cancellationToken) => Task.CompletedTask);

            app.MapPost("Post/{id}", (
                long id,
                [FromQuery] DateTime date,
                [FromBody] PostRequest request,
                CancellationToken cancellationToken) => Task.CompletedTask);

            app.MapPost("ThrowInternalServerError", () => ValueTask.FromException(new Exception("Sensitive data")));
        }

        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Test")]
        public record PostRequest(long Id2);
    }
}