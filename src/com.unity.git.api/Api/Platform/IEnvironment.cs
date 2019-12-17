using System;

namespace Unity.VersionControl.Git
{
    using IO;

    public interface IGitEnvironment : Unity.Editor.Tasks.IEnvironment
    {
        void Initialize(SPath extensionInstallPath);
        void InitializeRepository(SPath? expectedRepositoryPath = null);
        string GetSpecialFolder(Environment.SpecialFolder folder);

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
        IFileSystem FileSystem { get; set; }
        IUser User { get; set; }
        IRepository Repository { get; set; }
        ICacheContainer CacheContainer { get; }
        ISettings LocalSettings { get; }
        ISettings SystemSettings { get; }
        ISettings UserSettings { get; }
    }
}
