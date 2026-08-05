using Xunit;

namespace IntegrationTests
{
    public abstract class Test : IClassFixture<TestFixture>
    {
        public ApiClient.ApiClient ApiClient { get; private set; } = default!;
        internal TestFixture TestFixture { get; set; } = default!;

        public virtual ValueTask Initialize()
        {
            ApiClient = new(TestFixture.CreateClient());
            return ValueTask.CompletedTask;
        }
    }
}
