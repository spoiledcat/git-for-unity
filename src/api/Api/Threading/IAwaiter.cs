using System.Runtime.CompilerServices;

namespace Unity.Git
{
    interface IAwaiter : INotifyCompletion
    {
        bool IsCompleted { get; }
        void GetResult();
    }
}