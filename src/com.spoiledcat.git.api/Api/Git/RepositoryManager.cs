using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Logging;

namespace Unity.VersionControl.Git
{
    using IO;

    public interface IRepositoryManager : IDisposable
    {
        event Action<bool> IsBusyChanged;
        event Action<ConfigBranch?, ConfigRemote?, string> CurrentBranchUpdated;
        event Action<GitStatus> GitStatusUpdated;
        event Action<List<GitLock>> GitLocksUpdated;
        event Action<List<GitLogEntry>> GitLogUpdated;
        event Action<GitFileLog> GitFileLogUpdated;
        event Action<Dictionary<string, ConfigBranch>> LocalBranchesUpdated;
        event Action<Dictionary<string, ConfigRemote>, Dictionary<string, Dictionary<string, ConfigBranch>>> RemoteBranchesUpdated;
        event Action<GitAheadBehindStatus> GitAheadBehindStatusUpdated;

        event Action<CacheType> DataNeedsRefreshing;

        void Initialize();
        void Start();
        void Stop();
        ITask CommitAllFiles(string message, string body);
        ITask CommitFiles(List<string> files, string message, string body);
        ITask Fetch(string remote);
        ITask Pull(string remote, string branch);
        ITask Push(string remote, string branch);
        ITask Revert(string changeset);
        ITask RemoteAdd(string remote, string url);
        ITask RemoteRemove(string remote);
        ITask RemoteChange(string remote, string url);
        ITask SwitchBranch(string branch);
        ITask DeleteBranch(string branch, bool deleteUnmerged = false);
        ITask CreateBranch(string branch, string baseBranch);
        ITask LockFile(SPath file);
        ITask UnlockFile(SPath file, bool force);
        ITask DiscardChanges(GitStatusEntry[] gitStatusEntries);
        ITask CheckoutVersion(string changeset, IList<string> files);
        ITask UpdateGitLog();
        ITask UpdateGitStatus();
        ITask UpdateGitAheadBehindStatus();
        ITask UpdateLocks();
        ITask UpdateRepositoryInfo();
        ITask UpdateBranches();
        ITask UpdateFileLog(string path);


        int WaitForEvents();

        IGitConfig Config { get; }
        IGitClient GitClient { get; }
        bool IsBusy { get; }
    }

    public interface IRepositoryPathConfiguration
    {
        SPath RepositoryPath { get; }
        SPath DotGitPath { get; }
        SPath BranchesPath { get; }
        SPath RemotesPath { get; }
        SPath DotGitIndex { get; }
        SPath DotGitHead { get; }
        SPath DotGitConfig { get; }
        SPath WorktreeDotGitPath { get; }
        bool IsWorktree { get; }
    }

    public class RepositoryPathConfiguration : IRepositoryPathConfiguration
    {
        public RepositoryPathConfiguration(SPath repositoryPath)
        {
            RepositoryPath = repositoryPath;
            WorktreeDotGitPath = SPath.Default;

            DotGitPath = repositoryPath.Combine(".git");
            SPath commonPath;
            if (DotGitPath.FileExists())
            {
                DotGitPath =
                    DotGitPath.ReadAllLines()
                              .Where(x => x.StartsWith("gitdir:"))
                              .Select(x => x.Substring(7).Trim().ToSPath())
                              .First();
                if (DotGitPath.Combine("commondir").FileExists())
                {
                    commonPath = DotGitPath.Combine("commondir").ReadAllLines()
                        .Select(x => x.Trim().ToSPath())
                        .First();
                    commonPath = DotGitPath.Combine(commonPath);

                    IsWorktree = true;
                    WorktreeDotGitPath = commonPath;
                }
                else
                {
                    commonPath = DotGitPath;
                }
            }
            else
            {
                commonPath = DotGitPath;
            }

            BranchesPath = commonPath.Combine("refs", "heads");
            RemotesPath = commonPath.Combine("refs", "remotes");
            DotGitIndex = DotGitPath.Combine("index");
            DotGitHead = DotGitPath.Combine("HEAD");
            DotGitConfig = commonPath.Combine("config");
            DotGitCommitEditMsg = DotGitPath.Combine("COMMIT_EDITMSG");
        }

        public bool IsWorktree { get; }
        public SPath RepositoryPath { get; }
        public SPath WorktreeDotGitPath { get; }
        public SPath DotGitPath { get; }
        public SPath BranchesPath { get; }
        public SPath RemotesPath { get; }
        public SPath DotGitIndex { get; }
        public SPath DotGitHead { get; }
        public SPath DotGitConfig { get; }
        public SPath DotGitCommitEditMsg { get; }
    }

    public class RepositoryManager : IRepositoryManager
    {
        private readonly IGitConfig config;
        private readonly IGitClient gitClient;
        private readonly IRepositoryPathConfiguration repositoryPaths;
        private readonly IRepositoryWatcher watcher;
        private readonly CancellationTokenSource cts;
        private readonly ITaskManager taskManager;

        private bool isBusy;

        public event Action<ConfigBranch?, ConfigRemote?, string> CurrentBranchUpdated;
        public event Action<bool> IsBusyChanged;
        public event Action<GitStatus> GitStatusUpdated;
        public event Action<GitAheadBehindStatus> GitAheadBehindStatusUpdated;
        public event Action<List<GitLock>> GitLocksUpdated;
        public event Action<List<GitLogEntry>> GitLogUpdated;
        public event Action<GitFileLog> GitFileLogUpdated;
        public event Action<Dictionary<string, ConfigBranch>> LocalBranchesUpdated;
        public event Action<Dictionary<string, ConfigRemote>, Dictionary<string, Dictionary<string, ConfigBranch>>> RemoteBranchesUpdated;

        public event Action<CacheType> DataNeedsRefreshing;

        public RepositoryManager(ITaskManager taskManager,
            IGitConfig gitConfig,
            IRepositoryWatcher repositoryWatcher,
            IGitClient gitClient,
            IRepositoryPathConfiguration repositoryPaths)
        {
            this.taskManager = taskManager;
            cts = CancellationTokenSource.CreateLinkedTokenSource(taskManager.Token);
            this.repositoryPaths = repositoryPaths;
            this.gitClient = gitClient;
            this.watcher = repositoryWatcher;
            this.config = gitConfig;

            watcher.HeadChanged += WatcherOnHeadChanged;
            watcher.IndexChanged += WatcherOnIndexChanged;
            watcher.ConfigChanged += WatcherOnConfigChanged;
            watcher.RepositoryCommitted += WatcherOnRepositoryCommitted;
            watcher.RepositoryChanged += WatcherOnRepositoryChanged;
            watcher.LocalBranchesChanged += WatcherOnLocalBranchesChanged;
            watcher.RemoteBranchesChanged += WatcherOnRemoteBranchesChanged;
        }

        public static RepositoryManager CreateInstance(IPlatform platform, ITaskManager taskManager, IGitClient gitClient,
            SPath repositoryRoot)
        {
            var repositoryPathConfiguration = new RepositoryPathConfiguration(repositoryRoot);
            string filePath = repositoryPathConfiguration.DotGitConfig;
            var gitConfig = new GitConfig(filePath);

            var repositoryWatcher = new RepositoryWatcher(platform, repositoryPathConfiguration, taskManager.Token);

            return new RepositoryManager(taskManager, gitConfig, repositoryWatcher,
                gitClient, repositoryPathConfiguration);
        }

        public void Initialize()
        {
            watcher.Initialize();
        }

        public void Start()
        {
            watcher.Start();
        }

        public void Stop()
        {
            watcher.Stop();
        }

        /// <summary>
        /// Never ever call this from any callback that might be triggered by events
        /// raised here. This is not reentrancy safe and will deadlock if you do.
        /// Call this only from a non-callback main thread or preferably only for tests
        /// </summary>
        /// <returns></returns>
        public int WaitForEvents()
        {
            return watcher.CheckAndProcessEvents();
        }

        public ITask CommitAllFiles(string message, string body)
        {
            var task = GitClient.AddAll()
                .Then(GitClient.Commit(message, body));
            return HookupHandlers(task, true);
        }

        public ITask CommitFiles(List<string> files, string message, string body)
        {
            var task = GitClient.Add(files)
                .Then(GitClient.Commit(message, body));
            return HookupHandlers(task, true);
        }

        public ITask Fetch(string remote)
        {
            var task = GitClient.Fetch(remote);
            task.OnEnd += (_, __, success, ___) =>
            {
                if (success)
                    UpdateGitAheadBehindStatus().Start();
            };
            return HookupHandlers(task, false);
        }

        public ITask Pull(string remote, string branch)
        {
            var task = GitClient.Pull(remote, branch);
            return HookupHandlers(task, true);
        }

        public ITask Push(string remote, string branch)
        {
            var task = GitClient.Push(remote, branch);
            task.OnEnd += (_, __, success, ___) =>
            {
                if (success)
                    UpdateGitAheadBehindStatus().Start();
            };
            return HookupHandlers(task, false);
        }

        public ITask Revert(string changeset)
        {
            var task = GitClient.Revert(changeset);
            return HookupHandlers(task, true);
        }

        public ITask RemoteAdd(string remote, string url)
        {
            var task = GitClient.RemoteAdd(remote, url);
            return HookupHandlers(task, true);
        }

        public ITask RemoteRemove(string remote)
        {
            var task = GitClient.RemoteRemove(remote);
            return HookupHandlers(task, true);
        }

        public ITask RemoteChange(string remote, string url)
        {
            var task = GitClient.RemoteChange(remote, url);
            return HookupHandlers(task, true);
        }

        public ITask SwitchBranch(string branch)
        {
            var task = GitClient.SwitchBranch(branch);
            return HookupHandlers(task, true);
        }

        public ITask DeleteBranch(string branch, bool deleteUnmerged = false)
        {
            var task = GitClient.DeleteBranch(branch, deleteUnmerged);
            return HookupHandlers(task, false);
        }

        public ITask CreateBranch(string branch, string baseBranch)
        {
            var task = GitClient.CreateBranch(branch, baseBranch);
            return HookupHandlers(task, false);
        }

        public ITask LockFile(SPath file)
        {
            var task = GitClient.Lock(file)
                .Then(() => DataNeedsRefreshing?.Invoke(CacheType.GitLocks));
            return HookupHandlers(task, false);
        }

        public ITask UnlockFile(SPath file, bool force)
        {
            var task = GitClient.Unlock(file, force)
                .Then(() => DataNeedsRefreshing?.Invoke(CacheType.GitLocks));
            return HookupHandlers(task, false);
        }

        public ITask DiscardChanges(GitStatusEntry[] gitStatusEntries)
        {
            Guard.ArgumentNotNullOrEmpty(gitStatusEntries, "gitStatusEntries");

            ActionTask<GitStatusEntry[]> task = null;
            task = new ActionTask<GitStatusEntry[]>(taskManager, entries =>
                {
                    var itemsToDelete = new List<SPath>();
                    var itemsToRevert = new List<string>();

                    foreach (var gitStatusEntry in gitStatusEntries)
                    {
                        if (gitStatusEntry.WorkTreeStatus == GitFileStatus.Added || gitStatusEntry.WorkTreeStatus == GitFileStatus.Untracked)
                        {
                            itemsToDelete.Add(gitStatusEntry.path.ToSPath().MakeAbsolute());
                        }
                        else
                        {
                            itemsToRevert.Add(gitStatusEntry.path);
                        }
                    }

                    if (itemsToDelete.Any())
                    {
                        foreach (var itemToDelete in itemsToDelete)
                        {
                            itemToDelete.DeleteIfExists();
                        }
                    }

                    if (itemsToRevert.Any())
                    {
                        task.Then(GitClient.Discard(itemsToRevert));
                    }
                }
                , () => gitStatusEntries)
                { Message = "Discarding changes..." };

            return HookupHandlers(task, true);
        }

        public ITask CheckoutVersion(string changeset, IList<string> files)
        {
            var task = GitClient.CheckoutVersion(changeset, files)
                                .Then(() => DataNeedsRefreshing?.Invoke(CacheType.GitStatus));
            return HookupHandlers(task, false);
        }

        public ITask UpdateGitLog()
        {
            var task = GitClient.Log()
                .Then(logEntries =>
                {
                    GitLogUpdated?.Invoke(logEntries);
                });
            return HookupHandlers(task, false);
        }

        public ITask UpdateFileLog(string path)
        {
            var task = GitClient.LogFile(path)
                                .Then(logEntries =>
                                {
                                    var gitFileLog = new GitFileLog(path, logEntries);
                                    GitFileLogUpdated?.Invoke(gitFileLog);
                                });
            return HookupHandlers(task, false);
        }

        public ITask UpdateGitStatus()
        {
            var task = GitClient.Status()
                .Then(status =>
                {
                    GitStatusUpdated?.Invoke(status);
                });
            return HookupHandlers(task, false);
        }

        public ITask UpdateGitAheadBehindStatus()
        {
            ConfigBranch? configBranch;
            ConfigRemote? configRemote;
            GetCurrentBranchAndRemote(out configBranch, out configRemote);

            var updateTask = new ActionTask<GitAheadBehindStatus>(taskManager, status =>
                {
                    GitAheadBehindStatusUpdated?.Invoke(status);
                });
            if (configBranch.HasValue && configBranch.Value.Remote.HasValue)
            {
                var name = configBranch.Value.Name;
                var trackingName = configBranch.Value.IsTracking ? configBranch.Value.Remote.Value.Name + "/" + configBranch.Value.TrackingBranch : "[None]";

                var task = GitClient.AheadBehindStatus(name, trackingName)
                    .Then(updateTask);
                return HookupHandlers(task, false);
            }
            else
            {
                updateTask.PreviousResult = GitAheadBehindStatus.Default;
                return updateTask;
            }
        }

        public ITask UpdateLocks()
        {
            var task = GitClient.ListLocks(false)
                .Then(locks =>
                {
                    GitLocksUpdated?.Invoke(locks);
                });
            return HookupHandlers(task, false);

        }

        public ITask UpdateBranches()
        {
            var task = new ActionTask(taskManager, () =>
            {
                UpdateLocalBranches();
                UpdateRemoteBranches();
            })
            { Message = "Updating branches..." };
            return HookupHandlers(task, false);
        }

        public ITask UpdateRepositoryInfo()
        {
            var task = new ActionTask(taskManager, () =>
            {
                ConfigBranch? branch;
                ConfigRemote? remote;
                GetCurrentBranchAndRemote(out branch, out remote);
                var currentHead = GitClient.GetHead().RunSynchronously();
                CurrentBranchUpdated?.Invoke(branch, remote, currentHead);
            })
            { Message = "Updating repository info..." };
            return HookupHandlers(task, false);
        }

        private void GetCurrentBranchAndRemote(out ConfigBranch? branch, out ConfigRemote? remote)
        {
            branch = null;
            remote = null;

            var head = GetCurrentHead();
            if (head.StartsWith("ref:"))
            {
                var branchName = head.Substring(head.IndexOf("refs/heads/") + "refs/heads/".Length);
                branch = config.GetBranch(branchName);

                if (!branch.HasValue)
                {
                    branch = new ConfigBranch(branchName);
                }
            }

            var defaultRemote = "origin";

            if (branch.HasValue && branch.Value.IsTracking)
            {
                remote = branch.Value.Remote;
            }

            if (!remote.HasValue)
            {
                remote = config.GetRemote(defaultRemote);
            }

            if (!remote.HasValue)
            {
                var configRemotes = config.GetRemotes().ToArray();
                if (configRemotes.Any())
                {
                    remote = configRemotes.FirstOrDefault();
                }
            }
        }

        private ITask<T> HookupHandlers<T>(ITask<T> task, bool filesystemChangesExpected)
        {
            return (ITask<T>)HookupHandlers((ITask)task, filesystemChangesExpected);
        }

        private ITask HookupHandlers(ITask task, bool filesystemChangesExpected)
        {
            var isExclusive = task.IsChainExclusive();
            task.GetTopOfChain().OnStart += t =>
            {
                if (isExclusive)
                {
                    IsBusy = true;
                }

                if (filesystemChangesExpected)
                {
                    watcher.Stop();
                }
            };

            task.OnEnd += (_, __, ___) =>
            {
                if (filesystemChangesExpected)
                {
                    //Logger.Trace("Ended Operation - Enable Watcher");
                    watcher.Start();
                }

                if (isExclusive)
                {
                    //Logger.Trace("Ended Operation - Clearing Busy Flag");
                    IsBusy = false;
                }
            };
            task.Catch(_ =>
            {
                if (filesystemChangesExpected)
                {
                    //Logger.Trace("Ended Operation - Enable Watcher");
                    watcher.Start();
                }

                if (isExclusive)
                {
                    //Logger.Trace("Ended Operation - Clearing Busy Flag");
                    IsBusy = false;
                }

            });
            return task;
        }

        private string GetCurrentHead()
        {
            return repositoryPaths.DotGitHead.ReadAllLines().FirstOrDefault();
        }

        private void WatcherOnRemoteBranchesChanged()
        {
            DataNeedsRefreshing?.Invoke(CacheType.Branches);
            DataNeedsRefreshing?.Invoke(CacheType.GitAheadBehind);
        }

        private void WatcherOnLocalBranchesChanged()
        {
            DataNeedsRefreshing?.Invoke(CacheType.Branches);
            // the watcher should tell us what branch has changed so we can fire this only
            // when the active branch has changed
            DataNeedsRefreshing?.Invoke(CacheType.GitLog);
            DataNeedsRefreshing?.Invoke(CacheType.GitAheadBehind);
        }

        private void WatcherOnRepositoryCommitted()
        {
            DataNeedsRefreshing?.Invoke(CacheType.GitLog);
            DataNeedsRefreshing?.Invoke(CacheType.GitStatus);
        }

        private void WatcherOnRepositoryChanged()
        {
            DataNeedsRefreshing?.Invoke(CacheType.GitStatus);
        }

        private void WatcherOnConfigChanged()
        {
            config.Reset();
            DataNeedsRefreshing?.Invoke(CacheType.Branches);
            DataNeedsRefreshing?.Invoke(CacheType.RepositoryInfo);
            DataNeedsRefreshing?.Invoke(CacheType.GitLog);
        }

        private void WatcherOnHeadChanged()
        {
            DataNeedsRefreshing?.Invoke(CacheType.RepositoryInfo);
            DataNeedsRefreshing?.Invoke(CacheType.GitLog);
            DataNeedsRefreshing?.Invoke(CacheType.GitAheadBehind);
        }

        private void WatcherOnIndexChanged()
        {
            DataNeedsRefreshing?.Invoke(CacheType.GitStatus);
        }

        private void UpdateLocalBranches()
        {
            var branches = new Dictionary<string, ConfigBranch>();
            UpdateLocalBranches(branches, repositoryPaths.BranchesPath, config.GetBranches().Where(x => x.IsTracking), "");
            LocalBranchesUpdated?.Invoke(branches);
        }

        private void UpdateLocalBranches(Dictionary<string, ConfigBranch> branches, SPath path, IEnumerable<ConfigBranch> configBranches, string prefix)
        {
            foreach (var file in path.Files())
            {
                var branchName = prefix + file.FileName;
                var branch =
                    configBranches.Where(x => x.Name == branchName).Select(x => x as ConfigBranch?).FirstOrDefault();
                if (!branch.HasValue)
                {
                    branch = new ConfigBranch(branchName);
                }
                branches.Add(branchName, branch.Value);
            }

            foreach (var dir in path.Directories())
            {
                UpdateLocalBranches(branches, dir, configBranches, prefix + dir.FileName + "/");
            }
        }

        private void UpdateRemoteBranches()
        {
            var remotes = config.GetRemotes().ToArray().ToDictionary(x => x.Name, x => x);
            var remoteBranches = new Dictionary<string, Dictionary<string, ConfigBranch>>();

            foreach (var remote in remotes.Keys)
            {
                var branchList = new Dictionary<string, ConfigBranch>();
                var basedir = repositoryPaths.RemotesPath.Combine(remote);
                if (basedir.Exists())
                {
                    foreach (var branch in
                        basedir.Files(true)
                               .Select(x => x.RelativeTo(basedir))
                               .Select(x => x.ToString(SlashMode.Forward)))
                    {
                        branchList.Add(branch, new ConfigBranch(branch, remotes[remote], null));
                    }

                    remoteBranches.Add(remote, branchList);
                }
            }

            RemoteBranchesUpdated?.Invoke(remotes, remoteBranches);
            UpdateGitAheadBehindStatus();
        }

        private bool disposed;

        private void Dispose(bool disposing)
        {
            if (disposed) return;
            disposed = true;

            if (disposing)
            {
                CurrentBranchUpdated = null;
                GitStatusUpdated = null;
                GitAheadBehindStatusUpdated = null;
                GitLogUpdated = null;
                GitFileLogUpdated = null;
                GitLocksUpdated = null;
                LocalBranchesUpdated = null;
                RemoteBranchesUpdated = null;
                DataNeedsRefreshing = null;
                Stop();
                watcher.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IGitConfig Config => config;

        public IGitClient GitClient => gitClient;

        public bool IsBusy
        {
            get { return isBusy; }
            private set
            {
                if (isBusy != value)
                {
                    isBusy = value;
                    IsBusyChanged?.Invoke(isBusy);
                }
            }
        }

        protected static ILogging Logger { get; } = LogHelper.GetLogger<RepositoryManager>();
    }
}
