using Unity.VersionControl.Git;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.VersionControl.Git.Tasks;

namespace TestUtils
{
    static class ProcessManagerExtensions
    {
        static SPath defaultGitPath = "git".ToSPath();

        public static ITask<List<GitBranch>> GetGitBranches(this IProcessManager processManager,
            SPath workingDirectory,
            SPath? gitPath = null)
        {
            var processor = new BranchListOutputProcessor();

            return new GitListLocalBranchesTask(CancellationToken.None, processor)
                .Configure(processManager, gitPath ?? defaultGitPath, workingDirectory: workingDirectory);
        }

        public static ITask<List<GitLogEntry>> GetGitLogEntries(this IProcessManager processManager,
            SPath workingDirectory,
            IEnvironment environment,
            int logCount = 0,
            SPath? gitPath = null)
        {
            var gitStatusEntryFactory = new GitObjectFactory(environment);

            var processor = new LogEntryOutputProcessor(gitStatusEntryFactory);

            return new GitLogTask(logCount, null, CancellationToken.None, processor)
                .Configure(processManager, gitPath ?? defaultGitPath, workingDirectory: workingDirectory);
        }

        public static ITask<GitStatus> GetGitStatus(this IProcessManager processManager,
            SPath workingDirectory,
            IEnvironment environment,
            SPath? gitPath = null)
        {
            var gitStatusEntryFactory = new GitObjectFactory(environment);
            var processor = new GitStatusOutputProcessor(gitStatusEntryFactory);

            return new GitStatusTask(null, CancellationToken.None, processor)
                .Configure(processManager, workingDirectory: workingDirectory);
        }

        public static ITask<List<GitRemote>> GetGitRemoteEntries(this IProcessManager processManager,
            SPath workingDirectory,
            SPath? gitPath = null)
        {
            var processor = new RemoteListOutputProcessor();

            return new GitRemoteListTask(CancellationToken.None, processor)
                .Configure(processManager, gitPath ?? defaultGitPath, workingDirectory: workingDirectory);
        }

        public static ITask<string> GetGitCreds(this IProcessManager processManager,
            SPath workingDirectory,
            SPath? gitPath = null)
        {
            var processor = new FirstNonNullLineOutputProcessor();

            SPath path = gitPath ?? defaultGitPath;

            var task = new ProcessTask<string>(CancellationToken.None, processor)
                .Configure(processManager, path, "credential-wincred get", workingDirectory, true);

            task.OnStartProcess += p =>
            {
                p.StandardInput.WriteLine("protocol=https");
                p.StandardInput.WriteLine("host=github.com");
                p.StandardInput.Close();
            };
            return task;
        }
    }
}
