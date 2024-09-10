using System;
using System.Collections.Generic;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Helpers;

namespace Unity.VersionControl.Git
{
    using IO;

    /// <summary>
    /// Represents a repository, either local or retrieved via the GitHub API.
    /// </summary>
    public interface IRepository : IEquatable<IRepository>, IDisposable, IBackedByCache
    {
        void Initialize(IRepositoryManager theRepositoryManager, ITaskManager theTaskManager);
        void Start();

        ITask CommitAllFiles(string message, string body);
        ITask CommitFiles(List<string> files, string message, string body);
        ITask SetupRemote(string remoteName, string remoteUrl);
        ITask Pull();
        ITask Push();
        ITask Fetch();
        ITask Revert(string changeset);
        ITask RequestLock(SPath file);
        ITask ReleaseLock(SPath file, bool force);
        ITask DiscardChanges(GitStatusEntry[] discardEntries);
        ITask CheckoutVersion(string changeset, IList<string> files);

        /// <summary>
        /// Gets the name of the repository.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the repository clone URL.
        /// </summary>
        UriString CloneUrl { get; }

        /// <summary>
        /// Gets the name of the owner of the repository, taken from the clone URL.
        /// </summary>
        string Owner { get; }

        /// <summary>
        /// Gets the local path of the repository.
        /// </summary>
        SPath LocalPath { get; }
        bool IsGitHub { get; }
        /// <summary>
        /// Gets the current remote of the repository.
        /// </summary>
        GitRemote? CurrentRemote { get; }
        /// <summary>
        /// Gets the current branch of the repository.
        /// </summary>
        GitBranch? CurrentBranch { get; }
        int CurrentAhead { get; }
        int CurrentBehind { get; }
        List<GitStatusEntry> CurrentChanges { get; }
        GitRemote[] Remotes { get; }
        GitBranch[] LocalBranches { get; }
        GitBranch[] RemoteBranches { get; }
        List<GitLock> CurrentLocks { get; }
        string CurrentBranchName { get; }
        List<GitLogEntry> CurrentLog { get; }
        bool IsBusy { get; }
        string CurrentHead { get; }
        GitFileLog CurrentFileLog { get; }

        event Action<CacheUpdateEvent> LogChanged;
        event Action<CacheUpdateEvent> FileLogChanged;
        event Action<CacheUpdateEvent> TrackingStatusChanged;
        event Action<CacheUpdateEvent> StatusEntriesChanged;
        event Action<CacheUpdateEvent> CurrentBranchChanged;
        event Action<CacheUpdateEvent> CurrentRemoteChanged;
        event Action<CacheUpdateEvent> CurrentBranchAndRemoteChanged;
        event Action<CacheUpdateEvent> LocalBranchListChanged;
        event Action<CacheUpdateEvent> LocksChanged;
        event Action<CacheUpdateEvent> RemoteBranchListChanged;
        event Action<CacheUpdateEvent> LocalAndRemoteBranchListChanged;
        ITask RemoteAdd(string remote, string url);
        ITask RemoteRemove(string remote);
        ITask Push(string remote);
        ITask DeleteBranch(string branch, bool force);
        ITask CreateBranch(string branch, string baseBranch);
        ITask SwitchBranch(string branch);
        ITask UpdateFileLog(string path);
        void Refresh(CacheType cacheType);
        event Action<IProgress> OnProgress;
        ITask ConfigureMergeSettings(SPath unityYamlMergeExec, string keyName = "unityyamlmerge");
        ITask UpdateMergeSettings(SPath unityYamlMergeExec);
        ITask UpdateGitAttributes();
    }
}
