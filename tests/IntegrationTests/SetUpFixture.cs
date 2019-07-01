using System;
using NUnit.Framework;
using Unity.Git;

namespace IntegrationTests
{
    [SetUpFixture]
    public class SetUpFixture
    {
        [OneTimeSetUp]
        public void Setup()
        {
            LogHelper.TracingEnabled = true;

            LogHelper.LogAdapter = new MultipleLogAdapter(
                new FileLogAdapter($"..\\{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}-integration-tests.log")
                //, new ConsoleLogAdapter()
            );
        }
    }
}
