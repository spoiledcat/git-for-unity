using Unity.VersionControl.Git;
using Unity.VersionControl.Git.Tasks;
using System.Collections.Generic;
using System.Threading;
using Unity.VersionControl.Git.IO;
using Unity.Editor.Tasks;

namespace TestUtils
{
    static class ProcessManagerExtensions
    {
        static SPath defaultGitPath = "git".ToSPath();

        //public static Unity.Editor.Tasks.ITask<string> GetGitCreds(this IProcessManager processManager,
        //    SPath workingDirectory,
        //    SPath? gitPath = null)
        //{
        //    var processor = new FirstNonNullLineOutputProcessor();

        //    SPath path = gitPath ?? defaultGitPath;

        //    var task = new Unity.Editor.Tasks.ProcessTask<string>(CancellationToken.None, processor)
        //        .Configure(processManager, path, "credential-wincred get", workingDirectory, true);

        //    task.OnStartProcess += p =>
        //    {
        //        p.StandardInput.WriteLine("protocol=https");
        //        p.StandardInput.WriteLine("host=github.com");
        //        p.StandardInput.Close();
        //    };
        //    return task;
        //}
    }
}
