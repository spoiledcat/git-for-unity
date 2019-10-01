using System;
using System.Threading;
using UnityEditor;
using Unity.VersionControl.Git;

public class UnityUIThreadSynchronizationContext : SynchronizationContext, IMainThreadSynchronizationContext
{
    public void Schedule(Action action)
    {
        Guard.ArgumentNotNull(action, "action");
        Post(_ => action.SafeInvoke(), null);
    }

    public override void Post(SendOrPostCallback d, object state)
    {
        if (d == null)
            return;

        EditorApplication.delayCall += () => d(state);
    }
}
