using System;
using NUnit.Framework;
using Unity.Git;

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
