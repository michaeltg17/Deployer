using System.Net.Http.Json;
using ApiClient.Endpoints;
using Api.Models;

namespace ApiClient
{
    public class ApiClient(HttpClient httpClient)
    {
        public HttpClient HttpClient { get; } = httpClient;

        public TestEndpoints Test { get; } = new(httpClient);

        public Task<HttpResponseMessage> Deploy(DeployRequest request)
        {
            return httpClient.PostAsJsonAsync("/", request);
        }
    }
}