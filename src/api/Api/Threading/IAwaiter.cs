using System.Runtime.CompilerServices;

namespace Unity.VersionControl.Git
{
    interface IAwaiter : INotifyCompletion
    {
        bool IsCompleted { get; }
        void GetResult();
    }
}