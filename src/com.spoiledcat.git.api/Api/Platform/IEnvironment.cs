using System;

namespace Unity.VersionControl.Git
{
    using IO;
    public enum Folders
    {
        LocalApplicationData,
        CommonApplicationData,
        Logs
    }

    public interface IGitEnvironment : Unity.Editor.Tasks.IEnvironment
    {
        IGitEnvironment Initialize(SPath extensionInstallPath, string projectPath, string unityVersion = null, string EditorApplication_applicationPath = null, string EditorApplication_applicationContentsPath = null);
        void InitializeRepository(SPath? expectedRepositoryPath = null);
        SPath GetFolder(Folders folder);

        GitInstaller.GitInstallationState GitInstallationState { get; set; }
        GitInstaller.GitInstallDetails GitDefaultInstallation { get; set; }
        bool IsCustomGitExecutable { get; }
        SPath GitExecutablePath { get; }
        SPath GitInstallPath { get; }
        SPath GitLfsInstallPath { get; }
        SPath GitLfsExecutablePath { get; }
        SPath ExtensionInstallPath { get; }
        SPath RepositoryPath { get; }
        SPath UserCachePath { get; set; }
        SPath SystemCachePath { get; set; }
        SPath LocalAppData { get; }
        SPath CommonAppData { get; }
        SPath LogPath { get; }
        IUser User { get; set; }
        IRepository Repository { get; set; }
        ICacheContainer CacheContainer { get; }
        ISettings LocalSettings { get; }
        ISettings SystemSettings { get; }
        ISettings UserSettings { get; }
    }
}
