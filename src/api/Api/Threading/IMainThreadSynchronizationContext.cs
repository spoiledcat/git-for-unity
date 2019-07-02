using System;

namespace Unity.VersionControl.Git
{
    public interface IMainThreadSynchronizationContext
    {
        void Schedule(Action action);
    }
}
