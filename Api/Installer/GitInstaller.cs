using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Editor.Tasks.Helpers;
using Unity.Editor.Tasks.Logging;

namespace Unity.VersionControl.Git
{
    using Tasks;
    using IO;
    using Unity.Editor.Tasks;

    public class GitInstaller : TaskBase<GitInstaller.GitInstallationState>
    {
        private readonly CancellationToken cancellationToken;
        private readonly GitInstallDetails installDetails;
        private readonly IZipHelper sharpZipLibHelper;
        private readonly Dictionary<string, TaskData> tasks = new Dictionary<string, TaskData>();
        private readonly ProgressReporter progressReporter = new ProgressReporter();

        private readonly IPlatform platform;
        private GitInstallationState currentState;

        public GitInstaller(IPlatform platform,
            GitInstallationState state = null,
            GitInstallDetails installDetails = null,
            CancellationToken token = default)
            : base(platform.TaskManager, token)
        {
            this.platform = platform;
            this.currentState = state;
            this.sharpZipLibHelper = ZipHelper.Instance;
            this.cancellationToken = token;
            this.installDetails = installDetails ?? new GitInstallDetails(platform.Environment.UserCachePath, platform.Environment);
            progressReporter.OnProgress += progress.UpdateProgress;
        }

        protected override GitInstallationState RunWithReturn(bool success)
        {
            var ret = base.RunWithReturn(success);
            try
            {
                ret = SetupGitIfNeeded();
            }
            catch (Exception ex)
            {
                if (!RaiseFaultHandlers(ex))
                    Exception.Rethrow();
            }
            return ret;
        }

        private GitInstallationState SetupGitIfNeeded()
        {
            UpdateTask("Setting up git...", 100);

            bool skipSystemProbing = currentState != null;

            try
            {
                currentState = VerifyGitSettings(currentState);
                if (currentState.GitIsValid && currentState.GitLfsIsValid)
                {
                    Logger.Trace("Using git install path from settings: {0}", currentState.GitExecutablePath);
                    currentState.GitLastCheckTime = DateTimeOffset.Now;
                    return currentState;
                }

                //if (!skipSystemProbing)
                //{
                //    if (environment.IsMac)
                //        currentState = FindGit(currentState);
                //}

                currentState = SetDefaultPaths(currentState);
                currentState = CheckForGitUpdates(currentState);

                if (currentState.GitIsValid && currentState.GitLfsIsValid)
                {
                    currentState.GitLastCheckTime = DateTimeOffset.Now;
                    return currentState;
                }

                currentState = VerifyZipFiles(currentState);
                currentState = GetZipsIfNeeded(currentState);
                currentState = ExtractGit(currentState);

                if (!skipSystemProbing)
                {
                    // if installing from zip failed (internet down maybe?), try to find a usable system git
                    if (!currentState.GitIsValid &&
                        currentState.GitInstallationPath == installDetails.GitInstallationPath)
                        currentState = FindGit(currentState);
                    if (!currentState.GitLfsIsValid &&
                        currentState.GitLfsInstallationPath == installDetails.GitLfsInstallationPath)
                        currentState = FindGitLfs(currentState);
                }

                currentState.GitLastCheckTime = DateTimeOffset.Now;
                return currentState;
            }
            finally
            {
                UpdateTask("Setting up git...", 100);
            }
        }

        private void UpdateTask(string name, long value)
        {
            TaskData task = null;
            if (!tasks.TryGetValue(name, out task))
            {
                task = new TaskData(name, value);
                tasks.Add(name, task);
            }
            else
                task.UpdateProgress(value, task.progress.Total);
            progressReporter.UpdateProgress(task.progress);
        }

        public GitInstallationState VerifyGitSettings(GitInstallationState state = null)
        {
            state = state ?? platform.Environment.GitInstallationState;
            if (!state.GitExecutablePath.IsInitialized && !state.GitLfsExecutablePath.IsInitialized)
                return state;

            state = ValidateGitVersion(state);
            if (state.GitIsValid)
                state.GitInstallationPath = state.GitExecutablePath.Parent.Parent;

            if (!state.GitLfsExecutablePath.IsInitialized)
            {
                // look for it in the directory where we would install it from the bundle
                state.GitLfsExecutablePath = installDetails.GitLfsExecutablePath;
            }

            state = ValidateGitLfsVersion(state);

            if (state.GitLfsIsValid)
                state.GitLfsInstallationPath = state.GitLfsExecutablePath.Parent;

            return state;
        }

        public GitInstallationState FindSystemGit(GitInstallationState state)
        {
            state = FindGit(state);
            state = FindGitLfs(state);
            return state;
        }

        private GitInstallationState FindGit(GitInstallationState state)
        {
            if (!state.GitIsValid)
            {
                var gitPath = new FindExecTask(TaskManager,
                                  platform.ProcessManager.DefaultProcessEnvironment, platform.Environment,
                                  "git", cancellationToken)
                    .Configure(platform.ProcessManager)
                    .Progress(progressReporter.UpdateProgress)
                    .Catch(e => true)
                    .RunSynchronously();

                state.GitExecutablePath = gitPath;
                state = ValidateGitVersion(state);
                if (state.GitIsValid)
                    state.GitInstallationPath = gitPath.Parent.Parent;
            }
            return state;
        }

        private GitInstallationState FindGitLfs(GitInstallationState state)
        {
            if (!state.GitLfsIsValid)
            {
                var gitLfsPath = new FindExecTask(TaskManager,
                                  platform.ProcessManager.DefaultProcessEnvironment, platform.Environment,
                                  "git-lfs", cancellationToken)
                    .Configure(platform.ProcessManager)
                    .Progress(progressReporter.UpdateProgress)
                    .Catch(e => true)
                    .RunSynchronously();

                state.GitLfsExecutablePath = gitLfsPath;
                state = ValidateGitLfsVersion(state);
                if (state.GitLfsIsValid)
                    state.GitLfsInstallationPath = state.GitLfsExecutablePath.Parent;
                else
                {
                    // try somewhere else...
                    if (platform.Environment.IsWindows)
                    {
                        var potentialPath = state.GitInstallationPath.Combine(platform.Environment.Is32Bit ? "mingw32" : "mingw64", "bin", "git-lfs" + platform.Environment.ExecutableExtension);
                        if (potentialPath.FileExists())
                        {
                            state.GitLfsExecutablePath = potentialPath;
                            state = ValidateGitLfsVersion(state);
                            if (state.GitLfsIsValid)
                                state.GitLfsInstallationPath = state.GitLfsExecutablePath.Parent;
                        }
                    }
                }
            }
            return state;
        }

        public GitInstallationState SetDefaultPaths(GitInstallationState state)
        {
            if (!state.GitIsValid)
            {
                state = installDetails.GetDefaults();
                state = ValidateGitVersion(state);
            }
            return state;
        }

        public GitInstallationState ValidateGitVersion(GitInstallationState state)
        {
            if (!state.GitExecutablePath.IsInitialized || !state.GitExecutablePath.FileExists())
            {
                state.GitIsValid = false;
                return state;
            }
            var version = new GitVersionTask(TaskManager, platform.DefaultProcessEnvironment, state.GitExecutablePath)
                          .Configure(platform.ProcessManager)
                          .Progress(progressReporter.UpdateProgress)
                          .Catch(e => true)
                          .RunSynchronously();

            state.GitIsValid = version >= Constants.MinimumGitVersion;
            state.GitVersion = version;
            return state;
        }

        public GitInstallationState ValidateGitLfsVersion(GitInstallationState state)
        {
            if (!state.GitLfsExecutablePath.IsInitialized || !state.GitLfsExecutablePath.FileExists())
            {
                state.GitLfsIsValid = false;
                return state;
            }
            var version = new GitLfsVersionTask(TaskManager, platform.DefaultProcessEnvironment, state.GitLfsExecutablePath)
                    .Configure(platform.ProcessManager)
                    .Progress(progressReporter.UpdateProgress)
                    .Catch(e => true)
                    .RunSynchronously();
            state.GitLfsIsValid = version >= Constants.MinimumGitLfsVersion;
            state.GitLfsVersion = version;
            return state;
        }

        private GitInstallationState CheckForGitUpdates(GitInstallationState state)
        {
            if (state.IsCustomGitPath)
                return state;

            if (state.GitInstallationPath != installDetails.GitInstallationPath)
                return state;

            state.GitPackage = DugiteReleaseManifest.Load(TaskManager, installDetails.GitManifest,
                installDetails.GitManifestFeed, platform.Environment);
            if (state.GitPackage == null)
                return state;

            state.GitIsValid = state.GitVersion >= state.GitPackage.Version;
            if (!state.GitIsValid)
            {
                Logger.Trace($"{installDetails.GitExecutablePath} is out of date");
            }

            return state;
        }

        private GitInstallationState VerifyZipFiles(GitInstallationState state)
        {
            UpdateTask("Verifying package files", 100);
            try
            {
                if (state.GitIsValid || state.GitPackage == null)
                    return state;

                var asset = state.GitPackage.DugitePackage;
                state.GitZipPath = installDetails.ZipPath.Combine(asset.Name);
                state.GitZipExists = state.GitZipPath.FileExists();
                if (!Utils.VerifyFileIntegrity(state.GitZipPath, asset.Hash))
                {
                    state.GitZipPath.DeleteIfExists();
                }
                state.GitZipExists = state.GitZipPath.FileExists();
                return state;
            }
            finally
            {
                UpdateTask("Verifying package files", 100);
            }
        }

        private GitInstallationState GetZipsIfNeeded(GitInstallationState state)
        {
            if (state.GitZipExists || state.GitPackage == null)
                return state;

            var asset = state.GitPackage.DugitePackage;
            var downloader = new Downloader(TaskManager);
            downloader.QueueDownload(asset.Url, installDetails.ZipPath, asset.Name);

            downloader
                .Progress(progressReporter.UpdateProgress)
                .Catch(e =>
                {
                    Logger.Trace(e, "Failed to download");
                    return true;
                });

            downloader.RunSynchronously();

            state.GitZipExists = state.GitZipPath.IsInitialized && state.GitZipPath.FileExists();

            return state;
        }

        private GitInstallationState ExtractGit(GitInstallationState state)
        {
            var tempZipExtractPath = SPath.CreateTempDirectory("ghu_extract_git");

            if (state.GitZipExists && !state.GitIsValid)
            {
                var gitExtractPath = tempZipExtractPath.Combine("git").CreateDirectory();
                var unzipTask = new UnzipTask(TaskManager, state.GitZipPath,
                        gitExtractPath, sharpZipLibHelper)
                    .Progress(progressReporter.UpdateProgress)
                    .Catch(e =>
                    {
                        Logger.Trace(e, "Failed to unzip " + state.GitZipPath);
                        return true;
                    });

                unzipTask.RunSynchronously();
                var target = state.GitInstallationPath;
                if (unzipTask.Successful)
                {
                    Logger.Trace("Moving Git source:{0} target:{1}", gitExtractPath.ToString(), target.ToString());

                    CopyHelper.Copy(gitExtractPath, target);

                    state.GitIsValid = state.GitLfsIsValid = true;
                    state.IsCustomGitPath = state.GitExecutablePath != installDetails.GitExecutablePath;
                }
            }

            tempZipExtractPath.DeleteIfExists();
            return state;
        }

        public class GitInstallationState
        {
            private readonly Dictionary<string, object> fields = new Dictionary<string, object>();

            private T TryGetField<T>(string field)
            {
                if (fields.TryGetValue(field, out object val))
                {
                    return (T)val;
                }
                return default(T);
            }

            public bool GitIsValid { get => TryGetField<bool>(nameof(GitIsValid)); set => fields[nameof(GitIsValid)] = value; }
            public bool GitLfsIsValid { get => TryGetField<bool>(nameof(GitLfsIsValid)); set => fields[nameof(GitLfsIsValid)] = value; }
            public bool GitZipExists { get => TryGetField<bool>(nameof(GitZipExists)); set => fields[nameof(GitZipExists)] = value; }
            public SPath GitZipPath { get => TryGetField<SPath>(nameof(GitZipPath)); set => fields[nameof(GitZipPath)] = value; }
            public SPath GitInstallationPath { get => TryGetField<SPath>(nameof(GitInstallationPath)); set => fields[nameof(GitInstallationPath)] = value; }
            public SPath GitExecutablePath { get => TryGetField<SPath>(nameof(GitExecutablePath)); set => fields[nameof(GitExecutablePath)] = value; }
            public SPath GitLfsInstallationPath { get => TryGetField<SPath>(nameof(GitLfsInstallationPath)); set => fields[nameof(GitLfsInstallationPath)] = value; }
            public SPath GitLfsExecutablePath { get => TryGetField<SPath>(nameof(GitLfsExecutablePath)); set => fields[nameof(GitLfsExecutablePath)] = value; }
            public DugiteReleaseManifest GitPackage { get => TryGetField<DugiteReleaseManifest>(nameof(GitPackage)); set => fields[nameof(GitPackage)] = value; }
            public DateTimeOffset GitLastCheckTime { get; set; }
            public bool IsCustomGitPath { get => TryGetField<bool>(nameof(IsCustomGitPath)); set => fields[nameof(IsCustomGitPath)] = value; }
            public TheVersion GitVersion { get => TryGetField<TheVersion>(nameof(GitVersion)); set => fields[nameof(GitVersion)] = value; }
            public TheVersion GitLfsVersion { get => TryGetField<TheVersion>(nameof(GitLfsVersion)); set => fields[nameof(GitLfsVersion)] = value; }

            public GitInstallationState()
            {
                GitIsValid = GitLfsIsValid = GitZipExists = IsCustomGitPath = default(bool);
                GitZipPath = GitInstallationPath = GitExecutablePath = GitLfsInstallationPath = GitLfsExecutablePath = default(SPath);
                GitPackage = default(DugiteReleaseManifest);
                GitVersion = GitLfsVersion = default(TheVersion);
            }

            public override int GetHashCode()
            {
                int hash = 17;
                foreach (var val in fields.Values)
                {
                    hash *= 23 + (val?.GetHashCode() ?? 0);
                }
                return hash;
            }

            public override bool Equals(object other)
            {
                if (other is GitInstallationState state)
                    return Equals(state);
                return false;
            }

            public bool Equals(GitInstallationState other)
            {
                if ((object)other == null)
                    return false;
                return GetHashCode() == other.GetHashCode();
            }

            public static bool operator ==(GitInstallationState lhs, GitInstallationState rhs)
            {
                // If both are null, or both are same instance, return true.
                if (ReferenceEquals(lhs, rhs))
                    return true;

                // If one is null, but not both, return false.
                if (((object)lhs == null) || ((object)rhs == null))
                    return false;

                // Return true if the fields match:
                return Equals(lhs.fields, rhs.fields);
            }

            public static bool operator !=(GitInstallationState lhs, GitInstallationState rhs)
            {
                return !(lhs == rhs);
            }
        }

        public class GitInstallDetails
        {
            public const string ManifestName = "embedded-git.json";
            public const string ManifestFeed = "https://api.github.com/repos/github-for-unity/dugite-native/releases/latest";

            public const string GitDirectory = "git";

            public GitInstallDetails(SPath baseDataPath, IGitEnvironment environment)
            {
                ZipPath = baseDataPath.Combine("downloads");
                ZipPath.EnsureDirectoryExists();

                GitInstallationPath = baseDataPath.Combine(GitDirectory);
                GitExecutablePath = GitInstallationPath.Combine(environment.IsWindows ? "cmd" : "bin", "git" + environment.ExecutableExtension);

                GitLfsInstallationPath = GitLfsExecutablePath = GitInstallationPath;
                if (environment.IsWindows)
                    GitLfsExecutablePath = GitLfsInstallationPath.Combine(environment.Is32Bit ? "mingw32" : "mingw64");
                GitLfsExecutablePath = GitLfsExecutablePath.Combine("libexec", "git-core");
                GitLfsExecutablePath = GitLfsExecutablePath.Combine("git-lfs" + environment.ExecutableExtension);
                GitManifest = baseDataPath.Combine(GitManifestName);
            }

            public GitInstallationState GetDefaults()
            {
                return new GitInstallationState {
                    GitInstallationPath = GitInstallationPath,
                    GitExecutablePath = GitExecutablePath,
                    GitLfsInstallationPath = GitLfsInstallationPath,
                    GitLfsExecutablePath = GitLfsExecutablePath
                };
            }

            public SPath ZipPath { get; }
            public SPath GitInstallationPath { get; }
            public SPath GitLfsInstallationPath { get; }
            public SPath GitExecutablePath { get; }
            public SPath GitLfsExecutablePath { get; }
            public UriString GitManifestFeed { get; set;  } = ManifestFeed;
            public string GitManifestName { get; set; } = ManifestName;
            public SPath GitManifest { get; set; }

        }
    }
}
