using System;
using Unity.VersionControl.Git;
using System.Threading;

namespace TestUtils
{
    public class TestSynchronizationContext : SynchronizationContext, IMainThreadSynchronizationContext
    {
        public void Schedule(Action action)
        {
            action();
        }
    }
}
