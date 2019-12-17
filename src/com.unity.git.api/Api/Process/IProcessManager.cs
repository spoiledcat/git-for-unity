using System.Threading;

namespace Unity.VersionControl.Git
{
    using IO;

    public interface IProcessManager
    {
        T Configure<T>(T processTask, SPath? executable = null, string arguments = null, SPath? workingDirectory = null,
        	bool withInput = false, bool dontSetupGit = false)
            where T : IProcess;
        IProcess Reconnect(IProcess processTask, int i);
        CancellationToken CancellationToken { get; }
        void RunCommandLineWindow(SPath workingDirectory);
        void Stop();
    }
}
