using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using SpoiledCat.Git;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Logging;
using static Unity.VersionControl.Git.GitInstaller;

namespace Unity.VersionControl.Git
{
    using IO;

    public class ApplicationManagerBase : IApplicationManager
    {
        protected static ILogging Logger { get; } = LogHelper.GetLogger<IApplicationManager>();

        private RepositoryManager repositoryManager;
        private readonly ProgressReporter progressReporter = new ProgressReporter();
        private readonly Progress progress = new Progress(TaskBase.Default);
        protected bool isBusy;

        public event Action<IProgress> OnProgress
        {
            add => progressReporter.OnProgress += value;
            remove => progressReporter.OnProgress -= value;
        }

        public ApplicationManagerBase(SynchronizationContext synchronizationContext, IGitEnvironment environment)
        {
            Platform = new Platform(environment);
            Platform.Initialize(synchronizationContext);
        }

        public void Initialize()
        {
            LogHelper.TracingEnabled = UserSettings.Get(Constants.TraceLoggingKey, false);
            ApplicationConfiguration.Initialize(UserSettings);
            progress.OnProgress += progressReporter.UpdateProgress;
        }

        public void Run()
        {
            isBusy = true;
            progress.UpdateProgress(0, 100, "Initializing...");

            TaskManager.With(() =>
            {
                var state = new GitInstallationState();
                try
                {
                    if (Environment.IsMac)
                    {
                        var getEnvPath = new NativeProcessTask<string>(TaskManager, ProcessManager, "bash".ToSPath(),
                                "-c \"/usr/libexec/path_helper\"", new StringOutputProcessor())
                            .Catch(e => true); // make sure this doesn't throw if the task fails
                        var path = getEnvPath.RunSynchronously();
                        if (getEnvPath.Successful)
                        {
                            Logger.Trace("Existing Environment Path Original:{0} Updated:{1}", Environment.Path, path);
                            Environment.Path = path?.Split(new[] { "\"" }, StringSplitOptions.None)[1];
                        }
                    }

                    progress.UpdateProgress(50, 100, "Setting up git...");

                    state = Environment.GitInstallationState;
                    if (!state.GitIsValid && !state.GitLfsIsValid && FirstRun)
                    {
                        // importing old settings
                        var gitExecutablePath = Environment.SystemSettings.Get(Constants.GitInstallPathKey, SPath.Default);
                        if (gitExecutablePath.IsInitialized)
                        {
                            Environment.SystemSettings.Unset(Constants.GitInstallPathKey);
                            state.GitExecutablePath = gitExecutablePath;
                            state.GitInstallationPath = gitExecutablePath.Parent.Parent;
                            Environment.GitInstallationState = state;
                        }
                    }

                    var installer = new GitInstaller(Platform);
                    installer.Progress(progressReporter.UpdateProgress);
                    if (state.GitIsValid && state.GitLfsIsValid)
                    {
                        if (FirstRun)
                        {
                            installer.ValidateGitVersion(state);
                            if (state.GitIsValid)
                            {
                                installer.ValidateGitLfsVersion(state);
                            }
                        }
                    }

                    if (!state.GitIsValid || !state.GitLfsIsValid)
                    {
                        state = installer.RunSynchronously();
                    }

                    SetupGit(state);

                    progress.UpdateProgress(80, 100, "Initializing repository...");

                    if (state.GitIsValid && state.GitLfsIsValid)
                    {
                        RestartRepository();
                    }

                    progress.UpdateProgress(100, 100, "Initialized");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "A problem ocurred setting up Git");
                    progress.UpdateProgress(90, 100, "Initialization failed");
                }

                return state.GitIsValid && state.GitLfsIsValid;
            }, TaskAffinity.None)

                       .ThenInUI(gitIsValid =>
                       {
                           InitializationComplete();
                           if (gitIsValid)
                           {
                               InitializeUI();
                           }
                       })
                       .Start();
        }

        public void SetupGit(GitInstaller.GitInstallationState state)
        {
            if (!state.GitIsValid || !state.GitLfsIsValid)
            {
                if (!state.GitExecutablePath.IsInitialized)
                {
                    Logger.Warning(Localization.GitNotFound);
                }
                else if (!state.GitLfsExecutablePath.IsInitialized)
                {
                    Logger.Warning(Localization.GitLFSNotFound);
                }
                else if (state.GitVersion < Constants.MinimumGitVersion)
                {
                    Logger.Warning(String.Format(Localization.GitVersionTooLow, state.GitExecutablePath, state.GitVersion, Constants.MinimumGitVersion));
                }
                else if (state.GitLfsVersion < Constants.MinimumGitLfsVersion)
                {
                    Logger.Warning(String.Format(Localization.GitLfsVersionTooLow, state.GitLfsExecutablePath, state.GitLfsVersion, Constants.MinimumGitLfsVersion));
                }
                return;
            }

            Environment.GitInstallationState = state;
            Environment.User.Initialize(GitClient);

            if (!FirstRun) return;

            if (Environment.RepositoryPath.IsInitialized)
            {
                UpdateMergeSettings();

                GitClient.LfsInstall()
                         .Catch(e =>
                         {
                             Logger.Error(e, "Error running lfs install");
                             return true;
                         })
                         .RunSynchronously();
            }

            if (!Environment.IsWindows) return;

            var credentialHelper = GitClient.GetConfig("credential.helper", GitConfigSource.Global)
                                            .Catch(e =>
                                            {
                                                Logger.Error(e, "Error getting the credential helper");
                                                return true;
                                            }).RunSynchronously();

            if (string.IsNullOrEmpty(credentialHelper))
            {
                Logger.Warning("No Windows CredentialHelper found: Setting to wincred");
                GitClient.SetConfig("credential.helper", "wincred", GitConfigSource.Global)
                         .Catch(e =>
                         {
                             Logger.Error(e, "Error setting the credential helper");
                             return true;
                         })
                         .RunSynchronously();
            }
        }

        public void InitializeRepository()
        {
            isBusy = true;
            progress.UpdateProgress(0, 100, "Initializing...");

            TaskManager.With(() =>
            {
                var targetPath = Environment.UnityProjectPath.ToSPath();

                var gitignore = targetPath.Combine(".gitignore");
                var gitAttrs = targetPath.Combine(".gitattributes");
                var assetsGitignore = targetPath.Combine("Assets", ".gitignore");

                var filesForInitialCommit = new List<string> { gitignore, gitAttrs, assetsGitignore };

                GitClient.Init().RunSynchronously();
                progress.UpdateProgress(10, 100, "Initializing...");

                ConfigureMergeSettings();
                progress.UpdateProgress(20, 100, "Initializing...");

                GitClient.LfsInstall().RunSynchronously();
                progress.UpdateProgress(30, 100, "Initializing...");

                AssemblyResources.ToFile(ResourceType.Generic, "gitignore", gitignore, Environment);
                AssemblyResources.ToFile(ResourceType.Generic, "gitattributes", gitAttrs, Environment);

                assetsGitignore.CreateFile();

                GitClient.Add(filesForInitialCommit).RunSynchronously();
                progress.UpdateProgress(60, 100, "Initializing...");

                GitClient.Commit("Initial commit", null).RunSynchronously();
                progress.UpdateProgress(70, 100, "Initializing...");

                Environment.InitializeRepository();

                progress.UpdateProgress(90, 100, "Initializing...");
                RestartRepository();

            }, TaskAffinity.None)
                       .FinallyInUI((success, ex) =>
                       {
                           if (success)
                           {
                               InitializeUI();
                               progress.UpdateProgress(100, 100, "Initialized");
                           }
                           else
                           {
                               Logger.Error(ex, "A problem ocurred initializing the repository");
                               progress.UpdateProgress(100, 100, "Failed to initialize repository");
                           }
                           isBusy = false;
                       })
                .Start();
        }

        private void ConfigureMergeSettings(string keyName = null)
        {
            var unityYamlMergeExec =
                Environment.UnityApplicationContents.ToSPath().Combine("Tools", "UnityYAMLMerge" + Environment.ExecutableExtension);

            var yamlMergeCommand = $"'{unityYamlMergeExec}' merge -h -p --force %O %B %A %A";

            keyName = keyName ?? "unityyamlmerge";

            GitClient.SetConfig($"merge.{keyName}.name", "Unity SmartMerge (UnityYamlMerge)", GitConfigSource.Local).Catch(e => {
                Logger.Error(e, "Error setting merge." + keyName + ".name");
                return true;
            }).RunSynchronously();

            GitClient.SetConfig($"merge.{keyName}.driver", yamlMergeCommand, GitConfigSource.Local).Catch(e => {
                Logger.Error(e, "Error setting merge." + keyName + ".driver");
                return true;
            }).RunSynchronously();

            GitClient.SetConfig($"merge.{keyName}.recursive", "binary", GitConfigSource.Local).Catch(e => {
                Logger.Error(e, "Error setting merge." + keyName + ".recursive");
                return true;
            }).RunSynchronously();
        }

        private void UpdateMergeSettings()
        {
            var gitAttributesPath = Environment.RepositoryPath.Combine(".gitattributes");
            if (gitAttributesPath.FileExists())
            {
                var readAllText = gitAttributesPath.ReadAllText();
                var containsLegacyUnityYamlMergeError = readAllText.Contains("unityamlmerge");

                if (containsLegacyUnityYamlMergeError)
                {
                    ConfigureMergeSettings("unityamlmerge");
                }
            }

            GitClient.UnSetConfig("merge.unityyamlmerge.cmd", GitConfigSource.Local).Catch(e => {
                Logger.Error(e, "Error removing merge.unityyamlmerge.cmd");
                return true;
            }).RunSynchronously();

            GitClient.UnSetConfig("merge.unityyamlmerge.trustExitCode", GitConfigSource.Local).Catch(e => {
                Logger.Error(e, "Error removing merge.unityyamlmerge.trustExitCode");
                return true;
            }).RunSynchronously();

            ConfigureMergeSettings();
        }

        public void RestartRepository()
        {
            if (!Environment.RepositoryPath.IsInitialized)
                return;

            repositoryManager?.Dispose();

            repositoryManager = Unity.VersionControl.Git.RepositoryManager.CreateInstance(Platform, TaskManager, GitClient, Environment.RepositoryPath);
            repositoryManager.Initialize();
            Environment.Repository.Initialize(repositoryManager, TaskManager);
            repositoryManager.Start();
            Environment.Repository.Start();
            Logger.Trace($"Got a repository? {Environment.Repository?.LocalPath ?? "null"}");
        }

        public virtual void InitializeUI() {}
        protected virtual void InitializationComplete() {}

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (disposed) return;
                disposed = true;

                if (Platform is IDisposable platform)
                    platform.Dispose();

                if (repositoryManager != null)
                {
                    repositoryManager.Dispose();
                    repositoryManager = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public IPlatform Platform { get; protected set; }
        public IGitEnvironment Environment => Platform.Environment;
        public IProcessEnvironment GitEnvironment => ProcessManager.GitProcessEnvironment;
        public IGitProcessManager ProcessManager => Platform.ProcessManager;
        public ITaskManager TaskManager => Platform.TaskManager;
        public IGitClient GitClient => Platform.GitClient;
        public ISettings LocalSettings => Environment.LocalSettings;
        public ISettings SystemSettings => Environment.SystemSettings;
        public ISettings UserSettings => Environment.UserSettings;
        public bool IsBusy => isBusy;
        protected IRepositoryManager RepositoryManager => repositoryManager;
        protected bool FirstRun { get; set; }
        protected Guid InstanceId { get; set; }
    }
}
