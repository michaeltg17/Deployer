using System.Reflection;
using Xunit.DependencyInjection;

namespace IntegrationTests
{
    internal class BeforeAfterTestConfiguration(
        TestFixture testFixture,
        ITestOutputHelperAccessor testOutputHelperAccessor) : BeforeAfterTest
    {
        public override ValueTask BeforeAsync(object? testClassInstance, MethodInfo methodUnderTest)
        {
            if (testClassInstance is not Test test)
                return ValueTask.CompletedTask;

            testFixture.InjectableTestOutputSink.Inject(testOutputHelperAccessor.Output!);
            test.TestFixture = testFixture;
            return test.Initialize();
        }
    }
}
