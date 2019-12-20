using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git
{
    public interface IApplicationManager : IDisposable
    {
        IGitEnvironment Environment { get; }
        IPlatform Platform { get; }
        IProcessEnvironment GitEnvironment { get; }
        IGitProcessManager ProcessManager { get; }
        ISettings SystemSettings { get; }
        ISettings LocalSettings { get; }
        ISettings UserSettings { get; }
        ITaskManager TaskManager { get; }
        IGitClient GitClient { get; }
        bool IsBusy { get; }
        void Run();
        void InitializeRepository();
        event Action<IProgress> OnProgress;
        void SetupGit(GitInstaller.GitInstallationState state);
        void RestartRepository();
    }
}
