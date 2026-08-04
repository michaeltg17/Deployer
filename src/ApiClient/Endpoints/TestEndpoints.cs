using System.Net.Http.Json;

namespace ApiClient.Endpoints
{
    public class TestEndpoints(HttpClient httpClient)
    {
        const string BaseRoute = "/Test";

        public Task<HttpResponseMessage> ThrowInternalServerError()
        {
            return httpClient.PostAsync($"{BaseRoute}/ThrowInternalServerError", null);
        }

        public Task<HttpResponseMessage> GetOk()
        {
            return httpClient.GetAsync($"{BaseRoute}/GetOk");
        }

        public Task<HttpResponseMessage> RequestUnexistingRoute()
        {
            return httpClient.GetAsync("UnexistingRoute/UnexistingRoute");
        }
    }
}