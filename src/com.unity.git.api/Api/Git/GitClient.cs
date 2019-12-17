using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Editor.Tasks;
using Unity.VersionControl.Git.Tasks;
using static Unity.VersionControl.Git.GitInstaller;

namespace Unity.VersionControl.Git
{
    using IO;

    /// <summary>
    /// Client that provides access to git functionality
    /// </summary>
    public interface IGitClient
    {
        /// <summary>
        /// Executes `git init` to initialize a git repo.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Init();

        /// <summary>
        /// Executes `git lfs install` to install LFS hooks.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> LfsInstall();

        /// <summary>
        /// Executes `git rev-list` to determine the ahead/behind status between two refs.
        /// </summary>
        /// <param name="gitRef">Ref to compare</param>
        /// <param name="otherRef">Ref to compare against</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns><see cref="GitAheadBehindStatus"/> output</returns>
        ITask<GitAheadBehindStatus> AheadBehindStatus(string gitRef, string otherRef);

        /// <summary>
        /// Executes `git status` to determine the working directory status.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns><see cref="GitStatus"/> output</returns>
        ITask<GitStatus> Status();

        /// <summary>
        /// Executes `git config get` to get a configuration value.
        /// </summary>
        /// <param name="key">The configuration key to get</param>
        /// <param name="configSource">The config source (unspecified, local,user,global) to use</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> GetConfig(string key, GitConfigSource configSource);

        /// <summary>
        /// Executes `git config set` to set a configuration value.
        /// </summary>
        /// <param name="key">The configuration key to set</param>
        /// <param name="value">The value to set</param>
        /// <param name="configSource">The config source (unspecified, local,user,global) to use</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> SetConfig(string key, string value, GitConfigSource configSource);

        /// <summary>
        /// Executes `git config --unset` to remove a configuration value.
        /// </summary>
        /// <param name="key">The configuration key to remove</param>
        /// <param name="configSource">The config source (unspecified, local,user,global) to use</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> UnSetConfig(string key, GitConfigSource configSource);

        /// <summary>
        /// Executes two `git config get` commands to get the git user and email.
        /// </summary>
        /// <returns><see cref="GitUser"/> output</returns>
        ITask<GitUser> GetConfigUserAndEmail();

        /// <summary>
        /// Executes `git lfs locks` to get a list of lfs locks from the git lfs server.
        /// </summary>
        /// <param name="local"></param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns><see cref="List&lt;T&gt;"/> of <see cref="GitLock"/> output</returns>
        ITask<List<GitLock>> ListLocks(bool local);

        /// <summary>
        /// Executes `git pull` to perform a pull operation.
        /// </summary>
        /// <param name="remote">The remote to pull from</param>
        /// <param name="branch">The branch to pull</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Pull(string remote, string branch);

        /// <summary>
        /// Executes `git push` to perform a push operation.
        /// </summary>
        /// <param name="remote">The remote to push to</param>
        /// <param name="branch">The branch to push</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Push(string remote, string branch);

        /// <summary>
        /// Executes `git revert` to perform a revert operation.
        /// </summary>
        /// <param name="changeset">The changeset to revert</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Revert(string changeset);

        /// <summary>
        /// Executes `git reset` to perform a reset operation.
        /// </summary>
        /// <param name="changeset">The changeset to reset to</param>
        /// <param name="resetMode">Mode with which to reset with</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of the git command</returns>
        ITask<string> Reset(string changeset, GitResetMode resetMode = GitResetMode.NonSpecified);

        /// <summary>
        /// Executes `git fetch` to perform a fetch operation.
        /// </summary>
        /// <param name="remote">The remote to fetch from</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Fetch(string remote);

        /// <summary>
        /// Executes `git checkout` to switch branches.
        /// </summary>
        /// <param name="branch">The branch to checkout</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> SwitchBranch(string branch);

        /// <summary>
        /// Executes `git branch -d` to delete a branch.
        /// </summary>
        /// <param name="branch">The branch to delete</param>
        /// <param name="deleteUnmerged">The flag to indicate the branch should be deleted even if not merged</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> DeleteBranch(string branch, bool deleteUnmerged = false);

        /// <summary>
        /// Executes `git branch` to create a branch.
        /// </summary>
        /// <param name="branch">The name of branch to create</param>
        /// <param name="baseBranch">The name of branch to create from</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> CreateBranch(string branch, string baseBranch);

        /// <summary>
        /// Executes `git remote add` to add a git remote.
        /// </summary>
        /// <param name="remote">The remote to add</param>
        /// <param name="url">The url of the remote</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> RemoteAdd(string remote, string url);

        /// <summary>
        /// Executes `git remote rm` to remove a git remote.
        /// </summary>
        /// <param name="remote">The remote to remove</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> RemoteRemove(string remote);

        /// <summary>
        /// Executes `git remote set-url` to change the url of a git remote.
        /// </summary>
        /// <param name="remote">The remote to change</param>
        /// <param name="url">The url to change to</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> RemoteChange(string remote, string url);

        /// <summary>
        /// Executes `git commit` to perform a commit operation.
        /// </summary>
        /// <param name="message">The commit message summary</param>
        /// <param name="body">The commit message body</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Commit(string message, string body);

        /// <summary>
        /// Executes at least one `git add` command to add the list of files to the git index.
        /// </summary>
        /// <param name="files">The file to add</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Add(IList<string> files);

        /// <summary>
        /// Executes `git add -A` to add all files to the git index.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> AddAll();

        /// <summary>
        /// Executes at least one `git checkout` command to discard changes to the list of files.
        /// </summary>
        /// <param name="files">The files to discard</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Discard(IList<string> files);

        /// <summary>
        /// Executes `git checkout -- .` to discard all changes in the working directory.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> DiscardAll();

        /// <summary>
        /// Executes at least one `git clean` command to discard changes to the list of untracked files.
        /// </summary>
        /// <param name="files">The files to clean</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Clean(IList<string> files);

        /// <summary>
        /// Executes `git clean` command to discard changes to all untracked files.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> CleanAll();

        /// <summary>
        /// Executes at least one `git checkout` command to checkout files at the given changeset
        /// </summary>
        /// <param name="changeset">The md5 of the changeset</param>
        /// <param name="files">The files to check out</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> CheckoutVersion(string changeset, IList<string> files);

        /// <summary>
        /// Executes at least one `git reset HEAD` command to remove files from the git index.
        /// </summary>
        /// <param name="files">The files to remove</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Remove(IList<string> files);

        /// <summary>
        /// Executes at least one `git add` command to add the list of files to the git index. Followed by a `git commit` command to commit the changes.
        /// </summary>
        /// <param name="files">The files to add and commit</param>
        /// <param name="message">The commit message summary</param>
        /// <param name="body">The commit message body</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> AddAndCommit(IList<string> files, string message, string body);

        /// <summary>
        /// Executes `git lfs lock` to lock a file.
        /// </summary>
        /// <param name="file">The file to lock</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Lock(SPath file);

        /// <summary>
        /// Executes `git lfs unlock` to unlock a file.
        /// </summary>
        /// <param name="file">The file to unlock</param>
        /// <param name="force">If force should be used</param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> Unlock(SPath file, bool force);

        /// <summary>
        /// Executes `git log` to get the history of the current branch.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns><see cref="List&lt;T&gt;"/> of <see cref="GitLogEntry"/> output</returns>
        ITask<List<GitLogEntry>> Log();

        /// <summary>
        /// Executes `git log -- <file>` to get the history of a specific file.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns><see cref="List&lt;T&gt;"/> of <see cref="GitLogEntry"/> output</returns>
        ITask<List<GitLogEntry>> LogFile(string file);

        /// <summary>
        /// Executes `git --version` to get the git version.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns><see cref="TheVersion"/> output</returns>
        ITask<TheVersion> Version();

        /// <summary>
        /// Executes `git lfs version` to get the git lfs version.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns><see cref="TheVersion"/> output</returns>
        ITask<TheVersion> LfsVersion();

        /// <summary>
        /// Executes `git count-objects` to get the size of the git repo in kilobytes.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns><see cref="int"/> output</returns>
        ITask<int> CountObjects();

        /// <summary>
        /// Executes two `git set config` commands to set the git name and email.
        /// </summary>
        /// <param name="username">The username to set</param>
        /// <param name="email">The email to set</param>
        /// <param name="configSource">The source to set the values in</param>
        /// <returns><see cref="GitUser"/> output</returns>
        ITask<GitUser> SetConfigNameAndEmail(string username, string email, GitConfigSource configSource = GitConfigSource.User);

        /// <summary>
        /// Executes `git rev-parse --short HEAD` to get the current commit sha of the current branch.
        /// </summary>
        /// <param name="processor">A custom output processor instance</param>
        /// <returns>String output of git command</returns>
        ITask<string> GetHead();
    }

    public class GitClient : IGitClient
    {
        private const string UserNameConfigKey = "user.name";
        private const string UserEmailConfigKey = "user.email";
        private const int SpoolLength = 5000;
        private readonly ITaskManager taskManager;
        private readonly IGitEnvironment environment;
        private readonly IProcessManager processManager;
        private readonly IProcessEnvironment processEnvironment;
        private readonly CancellationTokenSource cts;
        private CancellationToken Token => cts.Token;

        public GitClient(ITaskManager taskManager, IProcessManager processManager, IProcessEnvironment gitProcessEnvironment, IGitEnvironment environment, CancellationToken cancellationToken = default)
        {
            this.taskManager = taskManager;
            this.environment = environment;
            this.processManager = processManager;
            this.processEnvironment = gitProcessEnvironment;
            cts = CancellationTokenSource.CreateLinkedTokenSource(taskManager.Token, cancellationToken);
        }

        public GitClient(IPlatform platform, CancellationToken cancellationToken = default)
        {
            this.taskManager = platform.TaskManager;
            this.environment = platform.Environment;
            this.processManager = platform.ProcessManager;
            this.processEnvironment = platform.ProcessEnvironment;

            cts = CancellationTokenSource.CreateLinkedTokenSource(taskManager.Token, cancellationToken);
        }

        ///<inheritdoc/>
        public ITask<string> Init()
        {
            return new GitInitTask(taskManager, processEnvironment, environment, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> LfsInstall()
        {
            return new GitLfsInstallTask(taskManager, processEnvironment, environment, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<GitStatus> Status()
        {
            return new GitStatusTask(taskManager, processEnvironment, environment, new GitObjectFactory(environment), Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<GitAheadBehindStatus> AheadBehindStatus(string gitRef, string otherRef)
        {
            return new GitAheadBehindStatusTask(taskManager, processEnvironment, environment, gitRef, otherRef, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<List<GitLogEntry>> Log()
        {
            return new GitLogTask(taskManager, processEnvironment, environment, new GitObjectFactory(environment), token: Token)
                .Configure(processManager)
                .Catch(exception => exception is ProcessException &&
                    exception.Message.StartsWith("fatal: your current branch") &&
                    exception.Message.EndsWith("does not have any commits yet"))
                .Then((success, _, list) => success ? list : new List<GitLogEntry>());
        }

        ///<inheritdoc/>
        public ITask<List<GitLogEntry>> LogFile(string file)
        {
            if (file == SPath.Default)
            {
                return taskManager.With(() => new List<GitLogEntry>(0));
            }

            return new GitLogTask(taskManager, processEnvironment, environment, new GitObjectFactory(environment), file, token: Token)
                .Configure(processManager)
                .Catch(exception => exception is ProcessException &&
                    exception.Message.StartsWith("fatal: your current branch") &&
                    exception.Message.EndsWith("does not have any commits yet"))
                .Then((success, _, list) => success ? list : new List<GitLogEntry>());
        }

        ///<inheritdoc/>
        public ITask<TheVersion> Version()
        {
            return new GitVersionTask(taskManager, processEnvironment, environment, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<TheVersion> LfsVersion()
        {
            return new GitLfsVersionTask(taskManager, processEnvironment, environment, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<int> CountObjects()
        {
            return new GitCountObjectsTask(taskManager, processEnvironment, environment, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> GetConfig(string key, GitConfigSource configSource)
        {
            return new GitConfigGetTask(taskManager, processEnvironment, environment, key, configSource, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> SetConfig(string key, string value, GitConfigSource configSource)
        {
            return new GitConfigSetTask(taskManager, processEnvironment, environment, key, value, configSource, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> UnSetConfig(string key, GitConfigSource configSource)
        {
            return new GitConfigUnSetTask(taskManager, processEnvironment, environment, key, configSource, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<GitUser> GetConfigUserAndEmail()
        {
            string username = null;
            string email = null;

            return GetConfig(UserNameConfigKey, GitConfigSource.User)
                .Then((success, _, value) => {
                    if (success)
                    {
                        username = value;
                    }
                })
                .Then(GetConfig(UserEmailConfigKey, GitConfigSource.User)
                    .Then((success, _, value) => {
                        if (success)
                        {
                            email = value;
                        }
                    }))
                .Then(() => new GitUser(username, email));
        }

        ///<inheritdoc/>
        public ITask<GitUser> SetConfigNameAndEmail(string username, string email, GitConfigSource configSource = GitConfigSource.User)
        {
            return SetConfig(UserNameConfigKey, username, configSource)
                .Then(SetConfig(UserEmailConfigKey, email, configSource))
                .Then(b => new GitUser(username, email));
        }

        ///<inheritdoc/>
        public ITask<List<GitLock>> ListLocks(bool local)
        {
            return new GitListLocksTask(taskManager, processEnvironment, environment, local, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> Pull(string remote, string branch)
        {
            return new GitPullTask(taskManager, processEnvironment, environment, remote, branch, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> Push(string remote, string branch)
        {
            return new GitPushTask(taskManager, processEnvironment, environment, remote, branch, true, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> Revert(string changeset)
        {
            return new GitRevertTask(taskManager, processEnvironment, environment, changeset, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> Reset(string changeset, GitResetMode resetMode = GitResetMode.NonSpecified)
        {
            return new GitResetTask(taskManager, processEnvironment, environment, changeset, resetMode, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> Fetch(string remote)
        {
            return new GitFetchTask(taskManager, processEnvironment, environment, remote, token: Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> SwitchBranch(string branch)
        {
            return new GitSwitchBranchesTask(taskManager, processEnvironment, environment, branch, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> DeleteBranch(string branch, bool deleteUnmerged = false)
        {
            return new GitBranchDeleteTask(taskManager, processEnvironment, environment, branch, deleteUnmerged, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> CreateBranch(string branch, string baseBranch)
        {
            return new GitBranchCreateTask(taskManager, processEnvironment, environment, branch, baseBranch, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> RemoteAdd(string remote, string url)
        {
            return new GitRemoteAddTask(taskManager, processEnvironment, environment, remote, url, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> RemoteRemove(string remote)
        {
            return new GitRemoteRemoveTask(taskManager, processEnvironment, environment, remote, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> RemoteChange(string remote, string url)
        {
            return new GitRemoteChangeTask(taskManager, processEnvironment, environment, remote, url, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> Commit(string message, string body)
        {
            return new GitCommitTask(taskManager, processEnvironment, environment, message, body, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> AddAll()
        {
            return new GitAddTask(taskManager, processEnvironment, environment, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> Add(IList<string> files)
        {
            GitAddTask last = null;
            foreach (var batch in files.Spool(SpoolLength))
            {
                var current = new GitAddTask(taskManager, processEnvironment, environment, batch, Token).Configure(processManager);
                if (last == null)
                {
                    last = current;
                }
                else
                {
                    last.Then(current);
                    last = current;
                }
            }

            return last;
        }

        ///<inheritdoc/>
        public ITask<string> Discard(IList<string> files)
        {
            GitCheckoutTask last = null;
            foreach (var batch in files.Spool(SpoolLength))
            {
                var current = new GitCheckoutTask(taskManager, processEnvironment, environment, batch, Token).Configure(processManager);
                if (last == null)
                {
                    last = current;
                }
                else
                {
                    last.Then(current);
                    last = current;
                }
            }

            return last;
        }

        ///<inheritdoc/>
        public ITask<string> DiscardAll()
        {
            return new GitCheckoutTask(taskManager, processEnvironment, environment, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> Clean(IList<string> files)
        {
            GitCleanTask last = null;
            foreach (var batch in files.Spool(SpoolLength))
            {
                var current = new GitCleanTask(taskManager, processEnvironment, environment, batch, Token).Configure(processManager);
                if (last == null)
                {
                    last = current;
                }
                else
                {
                    last.Then(current);
                    last = current;
                }
            }

            return last;
        }

        ///<inheritdoc/>
        public ITask<string> CleanAll()
        {
            return new GitCleanTask(taskManager, processEnvironment, environment, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> CheckoutVersion(string changeset, IList<string> files)
        {
            return new GitCheckoutTask(taskManager, processEnvironment, environment, changeset, files, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> Remove(IList<string> files)
        {
            GitRemoveFromIndexTask last = null;
            foreach (var batch in files.Spool(SpoolLength))
            {
                var current = new GitRemoveFromIndexTask(taskManager, processEnvironment, environment, batch, Token).Configure(processManager);
                if (last == null)
                {
                    last = current;
                }
                else
                {
                    last.Then(current);
                    last = current;
                }
            }

            return last;
        }

        ///<inheritdoc/>
        public ITask<string> AddAndCommit(IList<string> files, string message, string body)
        {
            return Add(files)
                .Then(new GitCommitTask(taskManager, processEnvironment, environment, message, body, Token)
                    .Configure(processManager));
        }

        ///<inheritdoc/>
        public ITask<string> Lock(SPath file)
        {
            return new GitLockTask(taskManager, processEnvironment, environment, file, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> Unlock(SPath file, bool force)
        {
            return new GitUnlockTask(taskManager, processEnvironment, environment, file, force, Token)
                .Configure(processManager);
        }

        ///<inheritdoc/>
        public ITask<string> GetHead()
        {
            return new NativeProcessTask<string>(taskManager, processEnvironment, environment.GitExecutablePath, "rev-parse --short HEAD",
                    new FirstNonNullOutputProcessor<string>(), Token) { Name = "Getting current head..." }
                   .Configure(processManager)
                      .Catch(exception =>
                          exception is ProcessException &&
                          exception.Message.StartsWith(
                              "fatal: your current branch") &&
                          exception.Message.EndsWith(
                              "does not have any commits yet"))
                      .Then((success, _, head) =>
                          success ? head : null);

        }

        protected static ILogging Logger { get; } = LogHelper.GetLogger<GitClient>();
    }

    [Serializable]
    public struct GitUser
    {
        public static GitUser Default = new GitUser();

        public string name;
        public string email;

        public string Name => name;
        public string Email { get { return String.IsNullOrEmpty(email) ? String.Empty : email; } }

        public GitUser(string name, string email)
        {
            this.name = name;
            this.email = email;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (name?.GetHashCode() ?? 0);
            hash = hash * 23 + Email.GetHashCode();
            return hash;
        }

        public override bool Equals(object other)
        {
            if (other is GitUser)
                return Equals((GitUser)other);
            return false;
        }

        public bool Equals(GitUser other)
        {
            return
                String.Equals(name, other.name) &&
                Email.Equals(other.Email);
        }

        public static bool operator ==(GitUser lhs, GitUser rhs)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(lhs, rhs))
                return true;

            // If one is null, but not both, return false.
            if (((object)lhs == null) || ((object)rhs == null))
                return false;

            // Return true if the fields match:
            return lhs.Equals(rhs);
        }

        public static bool operator !=(GitUser lhs, GitUser rhs)
        {
            return !(lhs == rhs);
        }

        public override string ToString()
        {
            return $"Name:\"{Name}\" Email:\"{Email}\"";
        }
    }
}
