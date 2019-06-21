using System;

namespace Unity.Git
{
    public interface IMainThreadSynchronizationContext
    {
        void Schedule(Action action);
    }
}
