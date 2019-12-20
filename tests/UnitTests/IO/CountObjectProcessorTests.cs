using TestUtils;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.VersionControl.Git;

namespace UnitTests
{
    [TestFixture]
    class GitCountObjectProcessorTests : BaseOutputProcessorTests
    {

        [Test]
        public void ShouldParseGitCountOutput()
        {
            var output = new[]
            {
                "2488 objects, 4237 kilobytes",
                null
            };

            AssertProcessOutput(output, 4237);
        }

        private void AssertProcessOutput(IEnumerable<string> lines, int expected)
        {
            int? result = null;
            var outputProcessor = new GitCountObjectsProcessor();
            outputProcessor.OnEntry += status => { result = status; };

            foreach (var line in lines)
            {
                outputProcessor.Process(line);
            }

            Assert.IsTrue(result.HasValue);
            Assert.AreEqual(expected, result.Value);
        }
    }
}
