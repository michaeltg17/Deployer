using System.Reflection;
using Xunit.DependencyInjection;

namespace IntegrationTests
{
    internal class BeforeAfterTestConfiguration(TestFixture testFixture) : BeforeAfterTest
    {
        public override ValueTask BeforeAsync(object? testClassInstance, MethodInfo methodUnderTest)
        {
            if (testClassInstance is not Test test)
                return ValueTask.CompletedTask;

            test.TestFixture = testFixture;
            return test.Initialize();
        }
    }
}
