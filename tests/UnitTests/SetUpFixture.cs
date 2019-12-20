using System;
using NUnit.Framework;
using Unity.Editor.Tasks.Logging;
using Unity.VersionControl.Git;

namespace UnitTests
{
    [SetUpFixture]
    public class SetUpFixture
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            LogHelper.TracingEnabled = true;

            LogHelper.LogAdapter = new MultipleLogAdapter(
                new FileLogAdapter($"..\\{DateTime.UtcNow:yyyyMMddHHmmss}-unit-tests.log")
                //, new ConsoleLogAdapter()
            );
        }
    }
}
