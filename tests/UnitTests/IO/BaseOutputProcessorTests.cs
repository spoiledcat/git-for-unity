using NUnit.Framework;
using TestUtils;

namespace UnitTests
{
    class BaseOutputProcessorTests
    {
        protected const string TestRootPath = @"c:\TestSource";
        protected TestSubstituteFactory SubstituteFactory { get; private set; }

        [OneTimeSetUp]
        public void TestFixtureSetup()
        {
            SubstituteFactory = new TestSubstituteFactory();
        }
    }
}
