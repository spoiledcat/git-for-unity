﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Helpers;
using Unity.Editor.Tasks.Logging;

namespace Unity.VersionControl.Git
{
    using IO;

    public interface IBackedByCache
    {
        void CheckAndRaiseEventsIfCacheNewer(CacheType cacheType, CacheUpdateEvent cacheUpdateEvent);
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Repository : IEquatable<Repository>, IRepository
    {
        private static ILogging Logger { get; } = LogHelper.GetLogger<Repository>();

        private IRepositoryManager repositoryManager;
        private ITaskManager taskManager;
        private ICacheContainer cacheContainer;
        private UriString cloneUrl;
        private string name;
        private HashSet<CacheType> cacheInvalidationRequests = new HashSet<CacheType>();
        private Dictionary<CacheType, Action<CacheUpdateEvent>> cacheUpdateEvents;
        private ProgressReporter progressReporter = new ProgressReporter();
        private string lastFileLog;

        public event Action<CacheUpdateEvent> LogChanged;
        public event Action<CacheUpdateEvent> FileLogChanged;
        public event Action<CacheUpdateEvent> TrackingStatusChanged;
        public event Action<CacheUpdateEvent> StatusEntriesChanged;
        public event Action<CacheUpdateEvent> CurrentBranchChanged;
        public event Action<CacheUpdateEvent> CurrentRemoteChanged;
        public event Action<CacheUpdateEvent> CurrentBranchAndRemoteChanged;
        public event Action<CacheUpdateEvent> LocalBranchListChanged;
        public event Action<CacheUpdateEvent> LocksChanged;
        public event Action<CacheUpdateEvent> RemoteBranchListChanged;
        public event Action<CacheUpdateEvent> LocalAndRemoteBranchListChanged;
        public event Action<IProgress> OnProgress
        {
            add { progressReporter.OnProgress += value; }
            remove { progressReporter.OnProgress -= value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Repository"/> class.
        /// </summary>
        /// <param name="localPath"></param>
        /// <param name="container"></param>
        public Repository(SPath localPath, ICacheContainer container)
        {
            Guard.ArgumentNotNull(localPath, nameof(localPath));

            LocalPath = localPath;

            cacheUpdateEvents = new Dictionary<CacheType, Action<CacheUpdateEvent>>
            {
                { CacheType.Branches, cacheUpdateEvent => {
                    LocalBranchListChanged?.Invoke(cacheUpdateEvent);
                    RemoteBranchListChanged?.Invoke(cacheUpdateEvent);
                    LocalAndRemoteBranchListChanged?.Invoke(cacheUpdateEvent);
                }},
                { CacheType.GitAheadBehind, c => TrackingStatusChanged?.Invoke(c) },
                { CacheType.GitLocks, c => LocksChanged?.Invoke(c) },
                { CacheType.GitLog, c => LogChanged?.Invoke(c) },
                { CacheType.GitFileLog, c => FileLogChanged?.Invoke(c) },
                { CacheType.GitStatus, c => StatusEntriesChanged?.Invoke(c) },
                { CacheType.GitUser, cacheUpdateEvent => { } },
                { CacheType.RepositoryInfo, cacheUpdateEvent => {
                    CurrentBranchChanged?.Invoke(cacheUpdateEvent);
                    CurrentRemoteChanged?.Invoke(cacheUpdateEvent);
                    CurrentBranchAndRemoteChanged?.Invoke(cacheUpdateEvent);
                }},
            };

            if (container == null)
                return;

            cacheContainer = container;
            cacheContainer.CacheInvalidated += CacheHasBeenInvalidated;
            cacheContainer.CacheUpdated += (cacheType, offset) =>
            {
                cacheUpdateEvents[cacheType](new CacheUpdateEvent(cacheType, offset));
            };
        }

        public void Initialize(IRepositoryManager theRepositoryManager, ITaskManager theTaskManager)
        {
            Guard.ArgumentNotNull(theRepositoryManager, nameof(theRepositoryManager));
            Guard.ArgumentNotNull(theTaskManager, nameof(theTaskManager));

            this.taskManager = theTaskManager;
            this.repositoryManager = theRepositoryManager;
            this.repositoryManager.CurrentBranchUpdated += RepositoryManagerOnCurrentBranchUpdated;
            this.repositoryManager.GitStatusUpdated += RepositoryManagerOnGitStatusUpdated;
            this.repositoryManager.GitAheadBehindStatusUpdated += RepositoryManagerOnGitAheadBehindStatusUpdated;
            this.repositoryManager.GitLogUpdated += RepositoryManagerOnGitLogUpdated;
            this.repositoryManager.GitFileLogUpdated += RepositoryManagerOnGitFileLogUpdated;
            this.repositoryManager.GitLocksUpdated += RepositoryManagerOnGitLocksUpdated;
            this.repositoryManager.LocalBranchesUpdated += RepositoryManagerOnLocalBranchesUpdated;
            this.repositoryManager.RemoteBranchesUpdated += RepositoryManagerOnRemoteBranchesUpdated;
            this.repositoryManager.DataNeedsRefreshing += RefreshCache;
            try
            {
                this.taskManager.OnProgress += progressReporter.UpdateProgress;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

        }

        public void Start()
        {
            foreach (var cacheType in cacheInvalidationRequests)
            {
                CacheHasBeenInvalidated(cacheType);
            }
        }

        public ITask SetupRemote(string remote, string remoteUrl)
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Guard.ArgumentNotNullOrWhiteSpace(remoteUrl, "remoteUrl");
            if (!CurrentRemote.HasValue || String.IsNullOrEmpty(CurrentRemote.Value.Name)) // there's no remote at all
            {
                return repositoryManager.RemoteAdd(remote, remoteUrl);
            }
            else
            {
                return repositoryManager.RemoteChange(remote, remoteUrl);
            }
        }

        public ITask CommitAllFiles(string message, string body) => repositoryManager.CommitAllFiles(message, body);
        public ITask CommitFiles(List<string> files, string message, string body) => repositoryManager.CommitFiles(files, message, body);
        public ITask Pull() => repositoryManager.Pull(CurrentRemote.Value.Name, CurrentBranch?.Name);
        public ITask Push(string remote) => repositoryManager.Push(remote, CurrentBranch?.Name);
        public ITask Push() => repositoryManager.Push(CurrentRemote.Value.Name, CurrentBranch?.Name);
        public ITask Fetch() => repositoryManager.Fetch(CurrentRemote.Value.Name);
        public ITask Revert(string changeset) => repositoryManager.Revert(changeset);
        public ITask RequestLock(SPath file) => repositoryManager.LockFile(file);
        public ITask ReleaseLock(SPath file, bool force) => repositoryManager.UnlockFile(file, force);
        public ITask DiscardChanges(GitStatusEntry[] gitStatusEntry) => repositoryManager.DiscardChanges(gitStatusEntry);
        public ITask CheckoutVersion(string changeset, IList<string> files) => repositoryManager.CheckoutVersion(changeset, files);
        public ITask RemoteAdd(string remote, string url) => repositoryManager.RemoteAdd(remote, url);
        public ITask RemoteRemove(string remote) => repositoryManager.RemoteRemove(remote);
        public ITask DeleteBranch(string branch, bool force) => repositoryManager.DeleteBranch(branch, force);
        public ITask CreateBranch(string branch, string baseBranch) => repositoryManager.CreateBranch(branch, baseBranch);
        public ITask SwitchBranch(string branch) => repositoryManager.SwitchBranch(branch);
        public ITask UpdateFileLog(string path)
        {
            lastFileLog = path;
            return repositoryManager.UpdateFileLog(path);
        }

        public void CheckAndRaiseEventsIfCacheNewer(CacheType cacheType, CacheUpdateEvent cacheUpdateEvent) => cacheContainer.CheckAndRaiseEventsIfCacheNewer(cacheType, cacheUpdateEvent);


        /// <summary>
        /// Note: We don't consider CloneUrl a part of the hash code because it can change during the lifetime
        /// of a repository. Equals takes care of any hash collisions because of this
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return LocalPath.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            var other = obj as Repository;
            return Equals(other);
        }

        public bool Equals(Repository other)
        {
            return Equals((IRepository)other);
        }

        public bool Equals(IRepository other)
        {
            if (ReferenceEquals(this, other))
                return true;

            return other != null && object.Equals(LocalPath, other.LocalPath);
        }

        private void RefreshCache(CacheType cacheType)
        {
            taskManager.RunInUI(() => Refresh(cacheType));
        }

        public void Refresh(CacheType cacheType)
        {
            var cache = cacheContainer.GetCache(cacheType);
            cache.InvalidateData();

            // take the opportunity to possibly refresh the locks cache, if it has timed out
            if (cacheType != CacheType.GitLocks)
            {
                cacheContainer.GetCache(CacheType.GitLocks).ValidateData();
            }
        }

        public ITask ConfigureMergeSettings(SPath unityYamlMergeExec, string keyName = "unityyamlmerge")
        {
            var yamlMergeCommand = $"'{unityYamlMergeExec}' merge -h -p --force %O %B %A %A";

            keyName = keyName ?? "unityyamlmerge";

            var task1 = repositoryManager.GitClient.SetConfig($"merge.{keyName}.name", "Unity SmartMerge (UnityYamlMerge)", GitConfigSource.Local).Catch(e => {
                Logger.Error(e, "Error setting merge." + keyName + ".name");
                return true;
            });

            var task2 = repositoryManager.GitClient.SetConfig($"merge.{keyName}.driver", yamlMergeCommand, GitConfigSource.Local).Catch(e => {
                Logger.Error(e, "Error setting merge." + keyName + ".driver");
                return true;
            });

            var task3 = repositoryManager.GitClient.SetConfig($"merge.{keyName}.recursive", "binary", GitConfigSource.Local).Catch(e => {
                Logger.Error(e, "Error setting merge." + keyName + ".recursive");
                return true;
            });

            return task1.Then(task2).Then(task3);
        }

        public ITask UpdateMergeSettings(SPath unityYamlMergeExec)
        {
            var task1 = repositoryManager.GitClient.UnSetConfig("merge.unityyamlmerge.cmd", GitConfigSource.Local).Catch(e => {
                Logger.Error(e, "Error removing merge.unityyamlmerge.cmd");
                return true;
            });

            var task2 = repositoryManager.GitClient.UnSetConfig("merge.unityyamlmerge.trustExitCode", GitConfigSource.Local).Catch(e => {
                Logger.Error(e, "Error removing merge.unityyamlmerge.trustExitCode");
                return true;
            });

            return task1.Then(task2).Then(ConfigureMergeSettings(unityYamlMergeExec));
        }

        public ITask UpdateGitAttributes()
        {
            return repositoryManager.Platform.TaskManager.With(() => {
                var gitAttrs = LocalPath.Combine(".gitattributes");
                AssemblyResources.ToFile(ResourceType.Generic, "gitattributes", gitAttrs,
                    repositoryManager.Platform.Environment);
            });
        }


        private void CacheHasBeenInvalidated(CacheType cacheType)
        {
            if (repositoryManager == null)
            {
                if (!cacheInvalidationRequests.Contains(cacheType))
                    cacheInvalidationRequests.Add(cacheType);
                return;
            }

            switch (cacheType)
            {
                case CacheType.Branches:
                    repositoryManager?.UpdateBranches().Catch(ex => InvalidationFailed(ex, cacheType)).Start();
                    break;

                case CacheType.GitLog:
                    repositoryManager?.UpdateGitLog().Catch(ex => InvalidationFailed(ex, cacheType)).Start();
                    break;

                case CacheType.GitFileLog:
                    repositoryManager?.UpdateFileLog(lastFileLog).Catch(ex => InvalidationFailed(ex, cacheType)).Start();
                    break;

                case CacheType.GitAheadBehind:
                    repositoryManager?.UpdateGitAheadBehindStatus().Catch(ex => InvalidationFailed(ex, cacheType)).Start();
                    break;

                case CacheType.GitLocks:
                    if (CurrentRemote != null)
                    {
                        repositoryManager?.UpdateLocks().Catch(ex => InvalidationFailed(ex, cacheType)).Start();
                    }
                    break;

                case CacheType.GitUser:
                    // user handles its own invalidation event
                    break;

                case CacheType.RepositoryInfo:
                    repositoryManager?.UpdateRepositoryInfo().Catch(ex => InvalidationFailed(ex, cacheType)).Start();
                    break;

                case CacheType.GitStatus:
                    repositoryManager?.UpdateGitStatus().Catch(ex => InvalidationFailed(ex, cacheType)).Start();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(cacheType), cacheType, null);
            }
        }

        private bool InvalidationFailed(Exception ex, CacheType cacheType)
        {
            Logger.Warning(ex, "Error invalidating {0}", cacheType);
            var managedCache = cacheContainer.GetCache(cacheType);
            managedCache.ResetInvalidation();
            return false;
        }


        private void RepositoryManagerOnCurrentBranchUpdated(ConfigBranch? branch, ConfigRemote? remote, string head)
        {
            taskManager.RunInUI(() =>
            {
                var data = new RepositoryInfoCacheData();
                data.CurrentConfigBranch = branch;
                data.CurrentGitBranch = branch.HasValue ? (GitBranch?)GetLocalGitBranch(branch.Value) : null;
                data.CurrentConfigRemote = remote;
                data.CurrentGitRemote = remote.HasValue ? (GitRemote?)GetGitRemote(remote.Value) : null;
                data.CurrentHead = head;
                name = null;
                cloneUrl = null;
                cacheContainer.RepositoryInfoCache.UpdateData(data);

                // force refresh of the Name and CloneUrl propertys
                var n = Name;
            });
        }

        private void RepositoryManagerOnGitStatusUpdated(GitStatus gitStatus)
        {
            taskManager.RunInUI(() =>
            {
                cacheContainer.GitStatusEntriesCache.Entries = gitStatus.Entries;
                cacheContainer.GitTrackingStatusCache.Ahead = gitStatus.Ahead;
                cacheContainer.GitTrackingStatusCache.Behind = gitStatus.Behind;
            });
        }

        private void RepositoryManagerOnGitAheadBehindStatusUpdated(GitAheadBehindStatus aheadBehindStatus)
        {
            taskManager.RunInUI(() =>
            {
                cacheContainer.GitTrackingStatusCache.Ahead = aheadBehindStatus.Ahead;
                cacheContainer.GitTrackingStatusCache.Behind = aheadBehindStatus.Behind;
            });
        }

        private void RepositoryManagerOnGitLogUpdated(List<GitLogEntry> gitLogEntries)
        {
            taskManager.RunInUI(() => cacheContainer.GitLogCache.Log = gitLogEntries);
        }

        private void RepositoryManagerOnGitFileLogUpdated(GitFileLog gitFileLog)
        {
            taskManager.RunInUI(() => cacheContainer.GitFileLogCache.FileLog = gitFileLog);
        }

        private void RepositoryManagerOnGitLocksUpdated(List<GitLock> gitLocks)
        {
            taskManager.RunInUI(() => cacheContainer.GitLocksCache.GitLocks = gitLocks);
        }

        private void RepositoryManagerOnRemoteBranchesUpdated(Dictionary<string, ConfigRemote> remoteConfigs,
            Dictionary<string, Dictionary<string, ConfigBranch>> remoteConfigBranches)
        {
            taskManager.RunInUI(() => {
                var gitRemotes = remoteConfigs.Values.Select(GetGitRemote).ToArray();
                var gitRemoteBranches = remoteConfigBranches.Values.SelectMany(x => x.Values).Select(GetRemoteGitBranch).ToArray();

                cacheContainer.BranchCache.SetRemotes(remoteConfigs, remoteConfigBranches, gitRemotes, gitRemoteBranches);
            });
        }

        private void RepositoryManagerOnLocalBranchesUpdated(Dictionary<string, ConfigBranch> localConfigBranchDictionary)
        {
            taskManager.RunInUI(() => {
                var gitLocalBranches = localConfigBranchDictionary.Values.Select(x => GetLocalGitBranch(x)).ToArray();
                cacheContainer.BranchCache.SetLocals(localConfigBranchDictionary, gitLocalBranches);
            });
        }

        private static GitBranch GetLocalGitBranch(ConfigBranch x)
        {
            var branchName = x.Name;
            var trackingName = x.IsTracking ? x.Remote.Value.Name + "/" + branchName : null;
            return new GitBranch(branchName, trackingName);
        }


        private bool disposed;
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    disposed = true;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }


        private static GitBranch GetRemoteGitBranch(ConfigBranch x) => new GitBranch(x.Remote.Value.Name + "/" + x.Name, "[None]");
        private static GitRemote GetGitRemote(ConfigRemote configRemote) => new GitRemote(configRemote.Name, configRemote.Url);

        public GitRemote[] Remotes => cacheContainer.BranchCache.Remotes;
        public GitBranch[] LocalBranches => cacheContainer.BranchCache.LocalBranches;
        public GitBranch[] RemoteBranches => cacheContainer.BranchCache.RemoteBranches;
        private ConfigBranch? CurrentConfigBranch => cacheContainer.RepositoryInfoCache.CurrentConfigBranch;
        private ConfigRemote? CurrentConfigRemote => cacheContainer.RepositoryInfoCache.CurrentConfigRemote;
        public int CurrentAhead => cacheContainer.GitTrackingStatusCache.Ahead;
        public int CurrentBehind => cacheContainer.GitTrackingStatusCache.Behind;
        public List<GitStatusEntry> CurrentChanges => cacheContainer.GitStatusEntriesCache.Entries;
        public GitBranch? CurrentBranch => cacheContainer.RepositoryInfoCache.CurrentGitBranch;
        public string CurrentBranchName => CurrentConfigBranch?.Name;
        public GitRemote? CurrentRemote => cacheContainer.RepositoryInfoCache.CurrentGitRemote;
        public List<GitLogEntry> CurrentLog => cacheContainer.GitLogCache.Log;
        public GitFileLog CurrentFileLog => cacheContainer.GitFileLogCache.FileLog;
        public List<GitLock> CurrentLocks => cacheContainer.GitLocksCache.GitLocks;
        public string CurrentHead => cacheContainer.RepositoryInfoCache.CurrentHead;

        public UriString CloneUrl
        {
            get
            {
                if (cloneUrl == null)
                {
                    var currentRemote = CurrentRemote;
                    if (currentRemote.HasValue && currentRemote.Value.Url != null)
                    {
                        cloneUrl = new UriString(currentRemote.Value.Url);
                    }
                }
                return cloneUrl;
            }
            private set
            {
                cloneUrl = value;
            }
        }

        public string Name
        {
            get
            {
                if (name == null)
                {
                    var url = CloneUrl;
                    if (url != null)
                    {
                        name = url.RepositoryName;
                    }
                    else
                    {
                        name = LocalPath.FileName;
                    }
                }
                return name;
            }
            private set { name = value; }
        }

        public SPath LocalPath { get; private set; }
        public string Owner => CloneUrl?.Owner ?? null;
        public bool IsGitHub => HostAddress.IsGitHubDotCom(CloneUrl);
        public bool IsBusy => repositoryManager?.IsBusy ?? false;

        internal string DebuggerDisplay => String.Format(CultureInfo.InvariantCulture,
            "{0} Owner: {1} Name: {2} CloneUrl: {3} LocalPath: {4} Branch: {5} Remote: {6}", GetHashCode(), Owner, Name,
            CloneUrl, LocalPath, CurrentBranch, CurrentRemote);
    }

    public interface IUser : IBackedByCache
    {
        string Name { get; }
        string Email { get; }
        event Action<CacheUpdateEvent> Changed;
        void Initialize(IGitClient client);
        void SetNameAndEmail(string name, string email);
    }

    [Serializable]
    public class User : IUser
    {
        private ICacheContainer cacheContainer;
        private IGitClient gitClient;
        private bool needsRefresh;

        public event Action<CacheUpdateEvent> Changed;

        public User(ICacheContainer cacheContainer)
        {
            if (cacheContainer != null)
            {
                this.cacheContainer = cacheContainer;
                cacheContainer.CacheInvalidated += (type) => { if (type == CacheType.GitUser) GitUserCacheOnCacheInvalidated(); };
                cacheContainer.CacheUpdated += (type, dt) => { if (type == CacheType.GitUser) CacheHasBeenUpdated(dt); };
            }
        }

        public void CheckAndRaiseEventsIfCacheNewer(CacheType cacheType, CacheUpdateEvent cacheUpdateEvent) => cacheContainer.CheckAndRaiseEventsIfCacheNewer(CacheType.GitUser, cacheUpdateEvent);

        public void Initialize(IGitClient client)
        {
            Guard.ArgumentNotNull(client, nameof(client));
            gitClient = client;
            if (needsRefresh)
            {
                needsRefresh = false;
                GitUserCacheOnCacheInvalidated();
            }
        }

        public void SetNameAndEmail(string name, string email)
        {
            gitClient.SetConfigNameAndEmail(name, email)
                     .ThenInUI(value => {
                         Name = value.Name;
                         Email = value.Email;
                     })
                     .Start();
        }

        public override string ToString()
        {
            return string.Format("Name: {0} Email: {1}", Name, Email);
        }

        private void CacheHasBeenUpdated(DateTimeOffset timeOffset)
        {
            HandleUserCacheUpdatedEvent(new CacheUpdateEvent(CacheType.GitUser, timeOffset));
        }

        private void GitUserCacheOnCacheInvalidated()
        {
            UpdateUserAndEmail();
        }

        private void HandleUserCacheUpdatedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            Changed?.Invoke(cacheUpdateEvent);
        }

        private void UpdateUserAndEmail()
        {
            if (gitClient == null)
            {
                needsRefresh = true;
                return;
            }

            gitClient.GetConfigUserAndEmail()
                     .Catch(InvalidationFailed)
                     .ThenInUI(value =>
                     {
                         Name = value.Name;
                         Email = value.Email;
                     }).Start();
        }

        private bool InvalidationFailed(Exception ex)
        {
            Logger.Warning(ex, "Error invalidating user cache");
            var managedCache = cacheContainer.GetCache(CacheType.GitUser);
            managedCache.ResetInvalidation();
            return false;
        }

        public string Name
        {
            get { return cacheContainer.GitUserCache.Name; }
            private set { cacheContainer.GitUserCache.Name = value; }
        }

        public string Email
        {
            get { return cacheContainer.GitUserCache.Email; }
            private set { cacheContainer.GitUserCache.Email = value; }
        }

        protected static ILogging Logger { get; } = LogHelper.GetLogger<User>();
    }
}
