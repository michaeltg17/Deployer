using Microsoft.Extensions.DependencyInjection;
using Xunit.DependencyInjection;

namespace IntegrationTests
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<BeforeAfterTest, BeforeAfterTestConfiguration>();
            services.AddSingleton<TestFixture>();
        }
    }
}