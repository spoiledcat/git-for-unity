using System;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git
{
    public static class ActionExtensions
    {
        public static void SafeInvoke(this Action action)
        {
            if (action != null)
                action();
        }

        public static void SafeInvoke<T>(this Action<T> action, T obj)
        {
            if (action != null)
                action(obj);
        }

        public static void SafeInvoke<T1, T2>(this Action<T1, T2> action, T1 obj, T2 obj2)
        {
            if (action != null)
                action(obj, obj2);
        }
    }

    public static class ProcessManagerExtensions
    {
        public static IProcessTask Configure(this IProcessTask task, IProcessManager processManager, string workingDirectory) => processManager.Configure(task, workingDirectory);
        public static IProcessTask<T> Configure<T>(this IProcessTask<T> task, IProcessManager processManager, string workingDirectory) => processManager.Configure(task, workingDirectory);
        public static IProcessTask<TData, T> Configure<TData, T>(this IProcessTask<TData, T> task, IProcessManager processManager, string workingDirectory) => processManager.Configure(task, workingDirectory);
    }
}
