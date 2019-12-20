using System;
using System.Threading;
using Unity.Editor.Tasks;

namespace TestUtils
{
    public class TestSynchronizationContext : SynchronizationContext, IMainThreadSynchronizationContext
    {
        public void Dispose()
        {
        }

        public void Schedule(Action action)
        {
            action();
        }
    }
}
